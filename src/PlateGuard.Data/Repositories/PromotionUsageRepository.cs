using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Helpers;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Data.Db;
using PlateGuard.Data.Mappers;

namespace PlateGuard.Data.Repositories;

public sealed class PromotionUsageRepository(PlateGuardDbContextFactory dbContextFactory) : RepositoryBase(dbContextFactory), IPromotionUsageRepository
{
    public async Task<PromotionUsage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        var entity = await dbContext.PromotionUsages
            .AsNoTracking()
            .FirstOrDefaultAsync(usage => usage.Id == id, cancellationToken);

        return entity is null ? null : PromotionUsageMapper.ToModel(entity);
    }

    public async Task<IReadOnlyList<PromotionUsage>> GetByVehicleIdAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        var entities = await dbContext.PromotionUsages
            .AsNoTracking()
            .Where(usage => usage.VehicleId == vehicleId)
            .OrderByDescending(usage => usage.ServiceDate)
            .ToListAsync(cancellationToken);

        return entities.Select(PromotionUsageMapper.ToModel).ToList();
    }

    public async Task<PromotionUsage?> GetByVehicleIdAndPromotionIdAsync(int vehicleId, int promotionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        var entity = await dbContext.PromotionUsages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                usage => usage.VehicleId == vehicleId && usage.PromotionId == promotionId,
                cancellationToken);

        return entity is null ? null : PromotionUsageMapper.ToModel(entity);
    }

    public async Task<IReadOnlyList<PromotionUsageRecord>> SearchRecordsAsync(PromotionUsageRecordQuery query, CancellationToken cancellationToken = default)
    {
        query ??= new PromotionUsageRecordQuery();

        await using var dbContext = CreateDbContext();
        var usageQuery = dbContext.PromotionUsages
            .AsNoTracking()
            .Include(usage => usage.Vehicle)
            .Include(usage => usage.Promotion)
            .AsQueryable();

        if (query.PromotionId.HasValue && query.PromotionId.Value > 0)
        {
            usageQuery = usageQuery.Where(usage => usage.PromotionId == query.PromotionId.Value);
        }

        if (query.DateFrom.HasValue)
        {
            var dateFrom = query.DateFrom.Value.Date;
            usageQuery = usageQuery.Where(usage => usage.ServiceDate >= dateFrom);
        }

        if (query.DateTo.HasValue)
        {
            var dateToExclusive = query.DateTo.Value.Date.AddDays(1);
            usageQuery = usageQuery.Where(usage => usage.ServiceDate < dateToExclusive);
        }

        var searchText = query.SearchText?.Trim();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var normalizedVehicleNumber = VehicleNumberNormalizer.Normalize(searchText);
            usageQuery = usageQuery.Where(usage =>
                EF.Functions.Like(usage.Vehicle.VehicleNumberRaw, $"%{searchText}%") ||
                (!string.IsNullOrWhiteSpace(normalizedVehicleNumber) &&
                 EF.Functions.Like(usage.Vehicle.VehicleNumberNormalized, $"%{normalizedVehicleNumber}%")) ||
                EF.Functions.Like(usage.Vehicle.PhoneNumber, $"%{searchText}%") ||
                (usage.Vehicle.OwnerName != null && EF.Functions.Like(usage.Vehicle.OwnerName, $"%{searchText}%")) ||
                EF.Functions.Like(usage.Promotion.PromotionName, $"%{searchText}%"));
        }

        var entities = await usageQuery
            .OrderByDescending(usage => usage.ServiceDate)
            .ThenByDescending(usage => usage.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(entity => new PromotionUsageRecord
        {
            PromotionUsageId = entity.Id,
            VehicleId = entity.VehicleId,
            PromotionId = entity.PromotionId,
            ServiceDate = entity.ServiceDate,
            VehicleNumberRaw = entity.Vehicle.VehicleNumberRaw,
            VehicleNumberNormalized = entity.Vehicle.VehicleNumberNormalized,
            PhoneNumber = entity.Vehicle.PhoneNumber,
            OwnerName = entity.Vehicle.OwnerName,
            Brand = entity.Vehicle.Brand,
            Model = entity.Vehicle.Model,
            PromotionName = entity.Promotion.PromotionName,
            PromotionIsActive = entity.Promotion.IsActive,
            Mileage = entity.Mileage,
            NormalPrice = entity.NormalPrice,
            DiscountedPrice = entity.DiscountedPrice,
            AmountPaid = entity.AmountPaid,
            Notes = entity.Notes
        }).ToList();
    }

    public async Task<int> CountByPromotionIdAsync(int promotionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.PromotionUsages
            .AsNoTracking()
            .CountAsync(usage => usage.PromotionId == promotionId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(int vehicleId, int promotionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.PromotionUsages
            .AsNoTracking()
            .AnyAsync(usage => usage.VehicleId == vehicleId && usage.PromotionId == promotionId, cancellationToken);
    }

    public async Task<PromotionUsage> AddAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var entity = PromotionUsageMapper.ToEntity(promotionUsage);
        entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;
        entity.ServiceDate = entity.ServiceDate == default ? DateTime.Today : entity.ServiceDate.Date;

        await dbContext.PromotionUsages.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return PromotionUsageMapper.ToModel(entity);
    }

    public async Task UpdateAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var entity = await dbContext.PromotionUsages.FirstOrDefaultAsync(existing => existing.Id == promotionUsage.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Promotion usage with id {promotionUsage.Id} was not found.");

        promotionUsage.ServiceDate = promotionUsage.ServiceDate.Date;
        promotionUsage.UpdatedAt = DateTime.UtcNow;
        PromotionUsageMapper.UpdateEntity(entity, promotionUsage);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var entity = await dbContext.PromotionUsages.FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.PromotionUsages.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
