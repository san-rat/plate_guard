using System;
using PlateGuard.Core.Models;

namespace PlateGuard.App.ViewModels;

public sealed class PromotionManagementItemViewModel(Promotion promotion, int usageCount)
{
    public Promotion Promotion { get; } = promotion;
    public int PromotionId => Promotion.Id;
    public string PromotionName => Promotion.PromotionName;
    public string Description => string.IsNullOrWhiteSpace(Promotion.Description) ? string.Empty : Promotion.Description.Trim();
    public bool IsActive => Promotion.IsActive;
    public int UsageCount { get; } = usageCount;
    public string StatusText => Promotion.IsActive ? "Active" : "Inactive";
    public string UsageCountText => $"{UsageCount} usage record(s)";

    public string ScheduleText =>
        (Promotion.StartDate, Promotion.EndDate) switch
        {
            (DateTime startDate, DateTime endDate) => $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            (DateTime startDate, null) => $"Starts {startDate:yyyy-MM-dd}",
            (null, DateTime endDate) => $"Ends {endDate:yyyy-MM-dd}",
            _ => "No date limits"
        };
}
