namespace PlateGuard.Core.Models;

public sealed class SavePromotionUsageRequest
{
    public string VehicleNumberRaw { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int PromotionId { get; set; }
    public DateTime? ServiceDate { get; set; }
    public int? Mileage { get; set; }
    public decimal? NormalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public decimal? AmountPaid { get; set; }
    public string? Notes { get; set; }
}
