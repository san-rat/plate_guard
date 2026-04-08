namespace PlateGuard.App.ViewModels;

public sealed class HistoryPromotionFilterOptionViewModel(int? promotionId, string label)
{
    public int? PromotionId { get; } = promotionId;
    public string Label { get; } = label;
}
