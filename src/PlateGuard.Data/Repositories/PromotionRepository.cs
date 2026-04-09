using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Data.Db;
using PlateGuard.Data.Mappers;

namespace PlateGuard.Data.Repositories;

public sealed class PromotionRepository(PlateGuardDbContextFactory dbContextFactory) : RepositoryBase(dbContextFactory), IPromotionRepository
{
    public async Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        var entity = await dbContext.Promotions
            .AsNoTracking()
            .FirstOrDefaultAsync(promotion => promotion.Id == id, cancellationToken);

        return entity is null ? null : PromotionMapper.ToModel(entity);
    }

    public async Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        var entities = await dbContext.Promotions
            .AsNoTracking()
            .OrderByDescending(promotion => promotion.IsActive)
            .ThenBy(promotion => promotion.PromotionName)
            .ToListAsync(cancellationToken);

        return entities.Select(PromotionMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<Promotion>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        var entities = await dbContext.Promotions
            .AsNoTracking()
            .Where(promotion => promotion.IsActive)
            .OrderBy(promotion => promotion.PromotionName)
            .ToListAsync(cancellationToken);

        return entities.Select(PromotionMapper.ToModel).ToList();
    }

    public async Task<Promotion> AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var entity = PromotionMapper.ToEntity(promotion);

        await dbContext.Promotions.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return PromotionMapper.ToModel(entity);
    }

    public async Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var entity = await dbContext.Promotions.FirstOrDefaultAsync(existing => existing.Id == promotion.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Promotion with id {promotion.Id} was not found.");

        promotion.UpdatedAt = DateTime.UtcNow;
        PromotionMapper.UpdateEntity(entity, promotion);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
