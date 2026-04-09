using PlateGuard.Core.Models;

namespace PlateGuard.Core.Interfaces;

public interface IPromotionUsageTransactionalWriter
{
    Task<SavePromotionUsageResult> SaveVehicleAndUsageAsync(
        Vehicle? existingVehicle,
        SavePromotionUsageRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateUsageRecordAsync(
        Vehicle vehicle,
        PromotionUsage promotionUsage,
        CancellationToken cancellationToken = default);
}
