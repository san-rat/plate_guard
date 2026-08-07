using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace PlateGuard.Cloud.Dtos;

[Table("promotion_usages")]
public sealed class PromotionUsageRow : BaseModel
{
    [PrimaryKey("sync_id")]
    public Guid SyncId { get; set; }

    [Column("vehicle_sync_id")]
    public Guid VehicleSyncId { get; set; }

    [Column("promotion_sync_id")]
    public Guid PromotionSyncId { get; set; }

    [Column("service_date")]
    public DateTime ServiceDate { get; set; }

    [Column("mileage")]
    public int? Mileage { get; set; }

    [Column("normal_price")]
    public decimal? NormalPrice { get; set; }

    [Column("discounted_price")]
    public decimal? DiscountedPrice { get; set; }

    [Column("amount_paid")]
    public decimal? AmountPaid { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at_utc")]
    public DateTime? DeletedAtUtc { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAtUtc { get; set; }
}
