namespace PlateGuard.Core.Models;

public sealed class ExportPromotionUsageRecordsResult : OperationResult
{
    public string? FilePath { get; set; }
    public int ExportedRecordCount { get; set; }
}
