namespace PlateGuard.Data.Entities;

public sealed class SyncConflictEntity
{
    public int Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public Guid LocalSyncId { get; set; }
    public Guid? CloudSyncId { get; set; }
    public string? VehicleNumberRaw { get; set; }
    public string? PromotionName { get; set; }
    public DateTime? ServiceDate { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime DetectedAtUtc { get; set; }
    public bool IsAcknowledged { get; set; }
}
