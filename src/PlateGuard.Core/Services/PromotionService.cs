using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;

namespace PlateGuard.Core.Services;

public sealed class PromotionService(IPromotionRepository promotionRepository) : IPromotionService
{
    private readonly IPromotionRepository _promotionRepository = promotionRepository;

    public Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _promotionRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _promotionRepository.GetAllAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Promotion>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return _promotionRepository.GetActiveAsync(cancellationToken);
    }

    public async Task<Promotion> CreateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        ValidatePromotion(promotion);
        promotion.CreatedAt = promotion.CreatedAt == default ? DateTime.UtcNow : promotion.CreatedAt;

        return await _promotionRepository.AddAsync(promotion, cancellationToken);
    }

    public async Task<Promotion> UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        ValidatePromotion(promotion);
        promotion.UpdatedAt = DateTime.UtcNow;

        await _promotionRepository.UpdateAsync(promotion, cancellationToken);
        return promotion;
    }

    public async Task<Promotion> ActivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await RequirePromotionAsync(id, cancellationToken);
        promotion.IsActive = true;

        return await UpdateAsync(promotion, cancellationToken);
    }

    public async Task<Promotion> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var promotion = await RequirePromotionAsync(id, cancellationToken);
        promotion.IsActive = false;

        return await UpdateAsync(promotion, cancellationToken);
    }

    private async Task<Promotion> RequirePromotionAsync(int id, CancellationToken cancellationToken)
    {
        return await _promotionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Promotion with id {id} was not found.");
    }

    private static void ValidatePromotion(Promotion promotion)
    {
        if (string.IsNullOrWhiteSpace(promotion.PromotionName))
        {
            throw new ArgumentException("Promotion name is required.", nameof(promotion));
        }
    }
}
