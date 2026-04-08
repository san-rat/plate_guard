namespace PlateGuard.Data.Entities;

public sealed class VehicleEntity
{
    public int Id { get; set; }
    public string VehicleNumberRaw { get; set; } = string.Empty;
    public string VehicleNumberNormalized { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<PromotionUsageEntity> PromotionUsages { get; set; } = [];
}
