namespace PlateGuard.Cloud;

public sealed class SyncConflictItem
{
    public int Id { get; init; }
    public string Kind { get; init; } = string.Empty;
    public string? VehicleNumberRaw { get; init; }
    public string? PromotionName { get; init; }
    public DateTime? ServiceDate { get; init; }
    public string Details { get; init; } = string.Empty;
    public DateTime DetectedAtUtc { get; init; }
}
