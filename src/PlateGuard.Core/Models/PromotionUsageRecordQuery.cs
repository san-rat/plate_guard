namespace PlateGuard.Core.Models;

public sealed class PromotionUsageRecordQuery
{
    public string? SearchText { get; set; }
    public int? PromotionId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
