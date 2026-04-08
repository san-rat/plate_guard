using System;
using PlateGuard.Core.Models;

namespace PlateGuard.App.ViewModels;

public sealed class HistoryRecordItemViewModel(PromotionUsageRecord record)
{
    public PromotionUsageRecord Record { get; } = record;
    public int PromotionUsageId => Record.PromotionUsageId;
    public int VehicleId => Record.VehicleId;
    public int PromotionId => Record.PromotionId;
    public string VehicleNumberDisplay => string.IsNullOrWhiteSpace(Record.VehicleNumberRaw)
        ? Record.VehicleNumberNormalized
        : Record.VehicleNumberRaw;
    public string VehicleNumberNormalized => Record.VehicleNumberNormalized;
    public string PhoneNumber => Record.PhoneNumber;
    public string OwnerName => string.IsNullOrWhiteSpace(Record.OwnerName) ? "-" : Record.OwnerName.Trim();
    public string PromotionName => Record.PromotionName;
    public string ServiceDateText => Record.ServiceDate.ToString("yyyy-MM-dd");
    public string AmountPaidText => Record.AmountPaid.HasValue ? Record.AmountPaid.Value.ToString("0.00") : "-";
    public string StatusText => Record.PromotionIsActive ? "Promotion active" : "Promotion inactive";
    public string BrandModelText => string.IsNullOrWhiteSpace(Record.Brand) && string.IsNullOrWhiteSpace(Record.Model)
        ? "-"
        : $"{Record.Brand} {Record.Model}".Trim();
    public string MileageText => Record.Mileage.HasValue ? Record.Mileage.Value.ToString() : "-";
    public string PricingText => $"Normal: {FormatMoney(Record.NormalPrice)} | Discounted: {FormatMoney(Record.DiscountedPrice)}";
    public string NotesText => string.IsNullOrWhiteSpace(Record.Notes) ? "-" : Record.Notes.Trim();

    private static string FormatMoney(decimal? value)
    {
        return value.HasValue ? value.Value.ToString("0.00") : "-";
    }
}
