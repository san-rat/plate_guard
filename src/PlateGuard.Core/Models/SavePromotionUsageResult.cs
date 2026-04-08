namespace PlateGuard.Core.Models;

public sealed class SavePromotionUsageResult : OperationResult
{
    public Vehicle? Vehicle { get; set; }
    public PromotionUsage? PromotionUsage { get; set; }
    public bool CreatedNewVehicle { get; set; }
}
