using Microsoft.EntityFrameworkCore;
using PlateGuard.Cloud.Dtos;
using PlateGuard.Data.Db;
using PlateGuard.Data.Entities;

namespace PlateGuard.Cloud;

public sealed class SyncEngine(
    ICloudSyncClient cloudSyncClient,
    PlateGuardDbContextFactory dbContextFactory,
    CloudSyncOptions options,
    ISyncLog syncLog,
    SyncReconciler? syncReconciler = null) : ISyncEngine
{
    private readonly ICloudSyncClient _cloudSyncClient = cloudSyncClient;
    private readonly PlateGuardDbContextFactory _dbContextFactory = dbContextFactory;
    private readonly CloudSyncOptions _options = options;
    private readonly ISyncLog _syncLog = syncLog;
    private readonly SyncReconciler? _syncReconciler = syncReconciler;
    private int _unconfiguredLogged;

    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            if (Interlocked.Exchange(ref _unconfiguredLogged, 1) == 0)
            {
                _syncLog.Info("Cloud sync is unconfigured; skipping sync.");
            }

            return new SyncResult { Status = SyncStatus.Unconfigured };
        }

        var result = new SyncResult();
        try
        {
            _syncLog.Info("Cloud sync started.");
            await _cloudSyncClient.SignInAsync(cancellationToken);

            await using var dbContext = _dbContextFactory.CreateDbContext([]);
            await PushVehiclesAsync(dbContext, result, cancellationToken);
            await PushPromotionsAsync(dbContext, result, cancellationToken);
            await PushPromotionUsagesAsync(dbContext, result, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var settings = await dbContext.Settings.SingleAsync(cancellationToken);
            var watermark = settings.LastSyncedAtUtc;
            DateTime? maximumServerUpdatedAtUtc = null;

            void ObserveServerTimestamp(DateTime? updatedAtUtc)
            {
                if (updatedAtUtc.HasValue &&
                    (!maximumServerUpdatedAtUtc.HasValue || updatedAtUtc.Value > maximumServerUpdatedAtUtc.Value))
                {
                    maximumServerUpdatedAtUtc = updatedAtUtc.Value;
                }
            }

            var vehicles = await _cloudSyncClient.FetchVehiclesAsync(watermark, cancellationToken);
            foreach (var row in vehicles)
            {
                ObserveServerTimestamp(row.UpdatedAtUtc);
                await PullVehicleAsync(dbContext, row, result, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var promotions = await _cloudSyncClient.FetchPromotionsAsync(watermark, cancellationToken);
            foreach (var row in promotions)
            {
                ObserveServerTimestamp(row.UpdatedAtUtc);
                await PullPromotionAsync(dbContext, row, result, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var promotionUsages = await _cloudSyncClient.FetchPromotionUsagesAsync(watermark, cancellationToken);
            foreach (var row in promotionUsages)
            {
                ObserveServerTimestamp(row.UpdatedAtUtc);
                await PullPromotionUsageAsync(dbContext, row, result, cancellationToken);
            }

            if (maximumServerUpdatedAtUtc.HasValue)
            {
                settings.LastSyncedAtUtc = maximumServerUpdatedAtUtc.Value;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            result.Status = SyncStatus.Success;
            _syncLog.Info($"Cloud sync finished: pushed {result.VehiclesPushed}/{result.PromotionsPushed}/{result.PromotionUsagesPushed}, pulled {result.VehiclesPulled}/{result.PromotionsPulled}/{result.PromotionUsagesPulled}, conflicts {result.Conflicts}, deferred {result.Deferred}.");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            result.Status = SyncStatus.Failed;
            result.ErrorMessage = exception.Message;
            _syncLog.Info($"Cloud sync unavailable: {exception.Message}");
            return result;
        }
    }

    private async Task PushVehiclesAsync(PlateGuardDbContext dbContext, SyncResult result, CancellationToken cancellationToken)
    {
        var entities = await dbContext.Vehicles.IgnoreQueryFilters().Where(entity => entity.IsDirty).ToListAsync(cancellationToken);
        foreach (var chunk in entities.Chunk(500))
        {
            try
            {
                var response = await _cloudSyncClient.UpsertVehiclesAsync(chunk.Select(ToRow).ToList(), cancellationToken);
                foreach (var entity in chunk)
                {
                    Accept(response, entity, result);
                }
            }
            catch (CloudUniqueConstraintException)
            {
                foreach (var entity in chunk)
                {
                    try
                    {
                        var response = await _cloudSyncClient.UpsertVehiclesAsync([ToRow(entity)], cancellationToken);
                        Accept(response, entity, result);
                    }
                    catch (CloudUniqueConstraintException)
                    {
                        result.Conflicts++;
                        if (_syncReconciler is not null)
                        {
                            await _syncReconciler.ResolveVehicleAsync(dbContext, entity, result, cancellationToken);
                        }
                    }
                }
            }
        }
    }

    private async Task PushPromotionsAsync(PlateGuardDbContext dbContext, SyncResult result, CancellationToken cancellationToken)
    {
        var entities = await dbContext.Promotions.IgnoreQueryFilters().Where(entity => entity.IsDirty).ToListAsync(cancellationToken);
        foreach (var chunk in entities.Chunk(500))
        {
            try
            {
                var response = await _cloudSyncClient.UpsertPromotionsAsync(chunk.Select(ToRow).ToList(), cancellationToken);
                foreach (var entity in chunk)
                {
                    if (TryAccept(response, entity.SyncId, out var updatedAtUtc)) { entity.IsDirty = false; entity.UpdatedAt = updatedAtUtc ?? entity.UpdatedAt; result.PromotionsPushed++; }
                }
            }
            catch (CloudUniqueConstraintException)
            {
                foreach (var entity in chunk)
                {
                    try { var response = await _cloudSyncClient.UpsertPromotionsAsync([ToRow(entity)], cancellationToken); if (TryAccept(response, entity.SyncId, out var updatedAtUtc)) { entity.IsDirty = false; entity.UpdatedAt = updatedAtUtc ?? entity.UpdatedAt; result.PromotionsPushed++; } }
                    catch (CloudUniqueConstraintException) { result.Conflicts++; result.ConflictsUnresolved++; }
                }
            }
        }
    }

    private async Task PushPromotionUsagesAsync(PlateGuardDbContext dbContext, SyncResult result, CancellationToken cancellationToken)
    {
        var entities = await dbContext.PromotionUsages.IgnoreQueryFilters().Where(entity => entity.IsDirty).ToListAsync(cancellationToken);
        foreach (var chunk in entities.Chunk(500))
        {
            try
            {
                var rows = new List<PromotionUsageRow>();
                foreach (var entity in chunk) { var vehicle = await dbContext.Vehicles.IgnoreQueryFilters().SingleAsync(v => v.Id == entity.VehicleId, cancellationToken); var promotion = await dbContext.Promotions.IgnoreQueryFilters().SingleAsync(p => p.Id == entity.PromotionId, cancellationToken); rows.Add(ToRow(entity, vehicle.SyncId, promotion.SyncId)); }
                var response = await _cloudSyncClient.UpsertPromotionUsagesAsync(rows, cancellationToken);
                foreach (var entity in chunk)
                {
                    if (TryAccept(response, entity.SyncId, out var updatedAtUtc)) { entity.IsDirty = false; entity.UpdatedAt = updatedAtUtc ?? entity.UpdatedAt; result.PromotionUsagesPushed++; }
                }
            }
            catch (CloudUniqueConstraintException)
            {
                foreach (var entity in chunk)
                {
                    try { var vehicle = await dbContext.Vehicles.IgnoreQueryFilters().SingleAsync(v => v.Id == entity.VehicleId, cancellationToken); var promotion = await dbContext.Promotions.IgnoreQueryFilters().SingleAsync(p => p.Id == entity.PromotionId, cancellationToken); var response = await _cloudSyncClient.UpsertPromotionUsagesAsync([ToRow(entity, vehicle.SyncId, promotion.SyncId)], cancellationToken); if (TryAccept(response, entity.SyncId, out var updatedAtUtc)) { entity.IsDirty = false; entity.UpdatedAt = updatedAtUtc ?? entity.UpdatedAt; result.PromotionUsagesPushed++; } }
                    catch (CloudUniqueConstraintException) { result.Conflicts++; if (_syncReconciler is not null) await _syncReconciler.ResolvePromotionUsageAsync(dbContext, entity, result, cancellationToken); }
                }
            }
        }
    }

    private static void Accept(CloudUpsertResult response, VehicleEntity entity, SyncResult result)
    {
        if (TryAccept(response, entity.SyncId, out var updatedAtUtc)) { entity.IsDirty = false; entity.UpdatedAt = updatedAtUtc ?? entity.UpdatedAt; result.VehiclesPushed++; }
    }

    private static async Task PullVehicleAsync(PlateGuardDbContext dbContext, VehicleRow row, SyncResult result, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Vehicles.IgnoreQueryFilters().SingleOrDefaultAsync(candidate => candidate.SyncId == row.SyncId, cancellationToken);
        if (entity is null)
        {
            await dbContext.Vehicles.AddAsync(new VehicleEntity
            {
                SyncId = row.SyncId,
                VehicleNumberRaw = row.VehicleNumberRaw,
                VehicleNumberNormalized = row.VehicleNumberNormalized,
                PhoneNumber = row.PhoneNumber,
                OwnerName = row.OwnerName,
                Brand = row.Brand,
                Model = row.Model,
                IsDeleted = row.IsDeleted,
                DeletedAtUtc = row.DeletedAtUtc,
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAtUtc,
                IsDirty = false
            }, cancellationToken);
            result.VehiclesPulled++;
            return;
        }

        if (IsNewer(row.UpdatedAtUtc, entity))
        {
            entity.VehicleNumberRaw = row.VehicleNumberRaw;
            entity.VehicleNumberNormalized = row.VehicleNumberNormalized;
            entity.PhoneNumber = row.PhoneNumber;
            entity.OwnerName = row.OwnerName;
            entity.Brand = row.Brand;
            entity.Model = row.Model;
            entity.IsDeleted = row.IsDeleted;
            entity.DeletedAtUtc = row.DeletedAtUtc;
            entity.CreatedAt = row.CreatedAt;
            entity.UpdatedAt = row.UpdatedAtUtc;
            entity.IsDirty = false;
            result.VehiclesPulled++;
        }
        else if (row.UpdatedAtUtc != entity.UpdatedAt)
        {
            entity.IsDirty = true;
        }
    }

    private static async Task PullPromotionAsync(PlateGuardDbContext dbContext, PromotionRow row, SyncResult result, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Promotions.IgnoreQueryFilters().SingleOrDefaultAsync(candidate => candidate.SyncId == row.SyncId, cancellationToken);
        if (entity is null)
        {
            await dbContext.Promotions.AddAsync(new PromotionEntity
            {
                SyncId = row.SyncId,
                PromotionName = row.PromotionName,
                Description = row.Description,
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                IsActive = row.IsActive,
                IsDeleted = row.IsDeleted,
                DeletedAtUtc = row.DeletedAtUtc,
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAtUtc,
                IsDirty = false
            }, cancellationToken);
            result.PromotionsPulled++;
            return;
        }

        if (IsNewer(row.UpdatedAtUtc, entity))
        {
            entity.PromotionName = row.PromotionName;
            entity.Description = row.Description;
            entity.StartDate = row.StartDate;
            entity.EndDate = row.EndDate;
            entity.IsActive = row.IsActive;
            entity.IsDeleted = row.IsDeleted;
            entity.DeletedAtUtc = row.DeletedAtUtc;
            entity.CreatedAt = row.CreatedAt;
            entity.UpdatedAt = row.UpdatedAtUtc;
            entity.IsDirty = false;
            result.PromotionsPulled++;
        }
        else if (row.UpdatedAtUtc != entity.UpdatedAt)
        {
            entity.IsDirty = true;
        }
    }

    private static async Task PullPromotionUsageAsync(PlateGuardDbContext dbContext, PromotionUsageRow row, SyncResult result, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.IgnoreQueryFilters().SingleOrDefaultAsync(candidate => candidate.SyncId == row.VehicleSyncId, cancellationToken);
        var promotion = await dbContext.Promotions.IgnoreQueryFilters().SingleOrDefaultAsync(candidate => candidate.SyncId == row.PromotionSyncId, cancellationToken);
        if (vehicle is null || promotion is null)
        {
            result.Deferred++;
            return;
        }

        var entity = await dbContext.PromotionUsages.IgnoreQueryFilters().SingleOrDefaultAsync(candidate => candidate.SyncId == row.SyncId, cancellationToken);
        if (entity is null)
        {
            await dbContext.PromotionUsages.AddAsync(new PromotionUsageEntity
            {
                SyncId = row.SyncId,
                VehicleId = vehicle.Id,
                PromotionId = promotion.Id,
                ServiceDate = row.ServiceDate,
                Mileage = row.Mileage,
                NormalPrice = row.NormalPrice,
                DiscountedPrice = row.DiscountedPrice,
                AmountPaid = row.AmountPaid,
                Notes = row.Notes,
                IsDeleted = row.IsDeleted,
                DeletedAtUtc = row.DeletedAtUtc,
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAtUtc,
                IsDirty = false
            }, cancellationToken);
            result.PromotionUsagesPulled++;
            return;
        }

        if (IsNewer(row.UpdatedAtUtc, entity))
        {
            entity.VehicleId = vehicle.Id;
            entity.PromotionId = promotion.Id;
            entity.ServiceDate = row.ServiceDate;
            entity.Mileage = row.Mileage;
            entity.NormalPrice = row.NormalPrice;
            entity.DiscountedPrice = row.DiscountedPrice;
            entity.AmountPaid = row.AmountPaid;
            entity.Notes = row.Notes;
            entity.IsDeleted = row.IsDeleted;
            entity.DeletedAtUtc = row.DeletedAtUtc;
            entity.CreatedAt = row.CreatedAt;
            entity.UpdatedAt = row.UpdatedAtUtc;
            entity.IsDirty = false;
            result.PromotionUsagesPulled++;
        }
        else if (row.UpdatedAtUtc != entity.UpdatedAt)
        {
            entity.IsDirty = true;
        }
    }

    private static bool TryAccept(CloudUpsertResult response, Guid syncId, out DateTime? updatedAtUtc)
    {
        var accepted = response.AcceptedRows.SingleOrDefault(row => row.SyncId == syncId);
        updatedAtUtc = accepted?.UpdatedAtUtc;
        return accepted is not null;
    }

    private static bool IsNewer(DateTime? cloudUpdatedAtUtc, VehicleEntity entity) => IsNewer(cloudUpdatedAtUtc, entity.UpdatedAt ?? entity.CreatedAt);
    private static bool IsNewer(DateTime? cloudUpdatedAtUtc, PromotionEntity entity) => IsNewer(cloudUpdatedAtUtc, entity.UpdatedAt ?? entity.CreatedAt);
    private static bool IsNewer(DateTime? cloudUpdatedAtUtc, PromotionUsageEntity entity) => IsNewer(cloudUpdatedAtUtc, entity.UpdatedAt ?? entity.CreatedAt);
    private static bool IsNewer(DateTime? cloudUpdatedAtUtc, DateTime localUpdatedAtUtc) => cloudUpdatedAtUtc.HasValue && cloudUpdatedAtUtc.Value > localUpdatedAtUtc;

    private static VehicleRow ToRow(VehicleEntity entity) => new()
    {
        SyncId = entity.SyncId,
        VehicleNumberRaw = entity.VehicleNumberRaw,
        VehicleNumberNormalized = entity.VehicleNumberNormalized,
        PhoneNumber = entity.PhoneNumber,
        OwnerName = entity.OwnerName,
        Brand = entity.Brand,
        Model = entity.Model,
        IsDeleted = entity.IsDeleted,
        DeletedAtUtc = entity.DeletedAtUtc,
        CreatedAt = entity.CreatedAt,
        UpdatedAtUtc = entity.UpdatedAt
    };

    private static PromotionRow ToRow(PromotionEntity entity) => new()
    {
        SyncId = entity.SyncId,
        PromotionName = entity.PromotionName,
        Description = entity.Description,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        IsActive = entity.IsActive,
        IsDeleted = entity.IsDeleted,
        DeletedAtUtc = entity.DeletedAtUtc,
        CreatedAt = entity.CreatedAt,
        UpdatedAtUtc = entity.UpdatedAt
    };

    private static PromotionUsageRow ToRow(PromotionUsageEntity entity, Guid vehicleSyncId, Guid promotionSyncId) => new()
    {
        SyncId = entity.SyncId,
        VehicleSyncId = vehicleSyncId,
        PromotionSyncId = promotionSyncId,
        ServiceDate = entity.ServiceDate,
        Mileage = entity.Mileage,
        NormalPrice = entity.NormalPrice,
        DiscountedPrice = entity.DiscountedPrice,
        AmountPaid = entity.AmountPaid,
        Notes = entity.Notes,
        IsDeleted = entity.IsDeleted,
        DeletedAtUtc = entity.DeletedAtUtc,
        CreatedAt = entity.CreatedAt,
        UpdatedAtUtc = entity.UpdatedAt
    };
}
