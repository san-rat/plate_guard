namespace PlateGuard.Data.Entities;

public sealed class PromotionUsageEntity
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public int PromotionId { get; set; }
    public DateTime ServiceDate { get; set; }
    public int? Mileage { get; set; }
    public decimal? NormalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public decimal? AmountPaid { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public VehicleEntity Vehicle { get; set; } = null!;
    public PromotionEntity Promotion { get; set; } = null!;
}
