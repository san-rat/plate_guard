using System;

namespace PlateGuard.App.ViewModels;

public sealed class UsageHistoryItemViewModel(string promotionName, DateTime serviceDate, string amountPaidText)
{
    public string PromotionName { get; } = promotionName;
    public DateTime ServiceDate { get; } = serviceDate;
    public string ServiceDateText { get; } = serviceDate.ToString("yyyy-MM-dd");
    public string AmountPaidText { get; } = amountPaidText;
}
