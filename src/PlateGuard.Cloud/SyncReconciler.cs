using Microsoft.EntityFrameworkCore;
using PlateGuard.Data.Db;
using PlateGuard.Data.Entities;

namespace PlateGuard.Cloud;

public sealed class SyncReconciler(ICloudSyncClient cloudSyncClient)
{
    private readonly ICloudSyncClient _cloudSyncClient = cloudSyncClient;

    public async Task ResolveVehicleAsync(PlateGuardDbContext dbContext, VehicleEntity local, SyncResult result, CancellationToken cancellationToken)
    {
        var cloud = await _cloudSyncClient.FindLiveVehicleByNormalizedNumberAsync(local.VehicleNumberNormalized, cancellationToken);
        if (cloud is null || cloud.SyncId == local.SyncId)
        {
            AddUnresolved(dbContext, local.SyncId, local.VehicleNumberRaw, null, null, "A duplicate vehicle rejection could not be matched to a live cloud vehicle.");
            result.ConflictsUnresolved++;
            return;
        }

        var survivor = await dbContext.Vehicles.IgnoreQueryFilters().SingleOrDefaultAsync(vehicle => vehicle.SyncId == cloud.SyncId, cancellationToken);
        if (survivor is not null)
        {
            var usages = await dbContext.PromotionUsages.IgnoreQueryFilters().Where(usage => usage.VehicleId == local.Id).ToListAsync(cancellationToken);
            var survivorPromotionIds = await dbContext.PromotionUsages.IgnoreQueryFilters().Where(usage => usage.VehicleId == survivor.Id && !usage.IsDeleted).Select(usage => usage.PromotionId).ToListAsync(cancellationToken);
            foreach (var usage in usages)
            {
                if (!usage.IsDeleted && survivorPromotionIds.Contains(usage.PromotionId))
                {
                    usage.IsDeleted = true;
                    usage.IsDirty = false;
                    usage.DeletedAtUtc ??= DateTime.UtcNow;
                    AddConflict(dbContext, "DuplicateRedemption", usage.SyncId, null, local.VehicleNumberRaw, null, usage.ServiceDate, "A duplicate redemption was dropped while merging duplicate vehicles.");
                }

                // Every child moves, dropped ones included: the foreign key is ON DELETE RESTRICT,
                // so a usage left pointing at the losing vehicle fails the whole sync. Soft-deleted
                // rows are outside the filtered unique index, so reparenting them cannot collide.
                usage.VehicleId = survivor.Id;
            }

            dbContext.Vehicles.Remove(local);
        }
        else
        {
            local.SyncId = cloud.SyncId;
            if (cloud.UpdatedAtUtc.HasValue && cloud.UpdatedAtUtc.Value > (local.UpdatedAt ?? local.CreatedAt))
            {
                local.VehicleNumberRaw = cloud.VehicleNumberRaw;
                local.VehicleNumberNormalized = cloud.VehicleNumberNormalized;
                local.PhoneNumber = cloud.PhoneNumber;
                local.OwnerName = cloud.OwnerName;
                local.Brand = cloud.Brand;
                local.Model = cloud.Model;
                local.IsDeleted = cloud.IsDeleted;
                local.DeletedAtUtc = cloud.DeletedAtUtc;
                local.UpdatedAt = cloud.UpdatedAtUtc;
                local.IsDirty = false;
            }
            else
            {
                local.IsDirty = true;
            }
        }

        AddConflict(dbContext, "DuplicateVehicle", local.SyncId, cloud.SyncId, local.VehicleNumberRaw, null, null, "Duplicate offline vehicle records were merged with the cloud vehicle.");
        result.ConflictsResolved++;
    }

    public async Task ResolvePromotionUsageAsync(PlateGuardDbContext dbContext, PromotionUsageEntity local, SyncResult result, CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.IgnoreQueryFilters().SingleOrDefaultAsync(entity => entity.Id == local.VehicleId, cancellationToken);
        var promotion = await dbContext.Promotions.IgnoreQueryFilters().SingleOrDefaultAsync(entity => entity.Id == local.PromotionId, cancellationToken);
        if (vehicle is null || promotion is null)
        {
            AddUnresolved(dbContext, local.SyncId, null, null, local.ServiceDate, "A duplicate redemption rejection had missing local parents.");
            result.ConflictsUnresolved++;
            return;
        }

        var cloud = await _cloudSyncClient.FindLivePromotionUsageAsync(vehicle.SyncId, promotion.SyncId, cancellationToken);
        if (cloud is null || cloud.SyncId == local.SyncId)
        {
            AddUnresolved(dbContext, local.SyncId, vehicle.VehicleNumberRaw, promotion.PromotionName, local.ServiceDate, "A duplicate redemption rejection could not be matched to a live cloud usage.");
            result.ConflictsUnresolved++;
            return;
        }

        local.IsDeleted = true;
        local.DeletedAtUtc ??= DateTime.UtcNow;
        local.IsDirty = false;
        AddConflict(dbContext, "DuplicateRedemption", local.SyncId, cloud.SyncId, vehicle.VehicleNumberRaw, promotion.PromotionName, local.ServiceDate, "The cloud redemption won; the local duplicate was dropped.");
        result.ConflictsResolved++;
    }

    private static void AddUnresolved(PlateGuardDbContext dbContext, Guid localSyncId, string? vehicleNumberRaw, string? promotionName, DateTime? serviceDate, string details)
    {
        AddConflict(dbContext, "Unresolved", localSyncId, null, vehicleNumberRaw, promotionName, serviceDate, details);
    }

    private static void AddConflict(PlateGuardDbContext dbContext, string kind, Guid localSyncId, Guid? cloudSyncId, string? vehicleNumberRaw, string? promotionName, DateTime? serviceDate, string details)
    {
        dbContext.SyncConflicts.Add(new SyncConflictEntity { Kind = kind, LocalSyncId = localSyncId, CloudSyncId = cloudSyncId, VehicleNumberRaw = vehicleNumberRaw, PromotionName = promotionName, ServiceDate = serviceDate, Details = details, DetectedAtUtc = DateTime.UtcNow });
    }
}
