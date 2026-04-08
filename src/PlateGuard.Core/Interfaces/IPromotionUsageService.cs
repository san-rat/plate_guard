using PlateGuard.Core.Models;

namespace PlateGuard.Core.Interfaces;

public interface IPromotionUsageService
{
    Task<EligibilityCheckResult> CheckEligibilityAsync(string vehicleNumber, int promotionId, CancellationToken cancellationToken = default);
    Task<EligibilityCheckResult> CheckEligibilityAsync(int vehicleId, int promotionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromotionUsage>> GetUsageHistoryForVehicleAsync(int vehicleId, CancellationToken cancellationToken = default);
    Task<int> GetUsageCountForPromotionAsync(int promotionId, CancellationToken cancellationToken = default);
    Task<SavePromotionUsageResult> SaveVehicleAndUsageAsync(SavePromotionUsageRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateUsageAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteUsageAsync(int id, string deletePassword, CancellationToken cancellationToken = default);
}
