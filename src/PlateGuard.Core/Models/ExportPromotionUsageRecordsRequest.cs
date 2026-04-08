namespace PlateGuard.Core.Models;

public sealed class ExportPromotionUsageRecordsRequest
{
    public IReadOnlyList<PromotionUsageRecord> Records { get; init; } = [];
    public string? ExportFolder { get; init; }
    public string FileNamePrefix { get; init; } = "plateguard-history";
}
