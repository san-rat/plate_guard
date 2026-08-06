namespace PlateGuard.Cloud;

public enum SyncStatus
{
    Success,
    Unconfigured,
    Failed
}

public sealed class SyncResult
{
    public SyncStatus Status { get; set; }
    public int VehiclesPushed { get; set; }
    public int PromotionsPushed { get; set; }
    public int PromotionUsagesPushed { get; set; }
    public int VehiclesPulled { get; set; }
    public int PromotionsPulled { get; set; }
    public int PromotionUsagesPulled { get; set; }
    public int Conflicts { get; set; }
    public int Deferred { get; set; }
    public string? ErrorMessage { get; set; }
}
