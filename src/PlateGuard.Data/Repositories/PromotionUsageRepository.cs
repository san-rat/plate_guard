using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Data.Db;
using PlateGuard.Data.Mappers;

namespace PlateGuard.Data.Repositories;

public sealed class PromotionUsageRepository() : RepositoryBase(new PlateGuardDbContextFactory()), IPromotionUsageRepository
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
        entity.ServiceDate = entity.ServiceDate == default ? DateTime.UtcNow : entity.ServiceDate;

        await dbContext.PromotionUsages.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return PromotionUsageMapper.ToModel(entity);
    }

    public async Task UpdateAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var entity = await dbContext.PromotionUsages.FirstOrDefaultAsync(existing => existing.Id == promotionUsage.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Promotion usage with id {promotionUsage.Id} was not found.");

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
