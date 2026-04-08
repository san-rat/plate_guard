using PlateGuard.Core.Models;

namespace PlateGuard.Core.Interfaces;

public interface IPromotionUsageRepository
{
    Task<PromotionUsage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromotionUsage>> GetByVehicleIdAsync(int vehicleId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int vehicleId, int promotionId, CancellationToken cancellationToken = default);
    Task<PromotionUsage> AddAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default);
    Task UpdateAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
