using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace PlateGuard.Cloud.Dtos;

[Table("promotions")]
public sealed class PromotionRow : BaseModel
{
    [PrimaryKey("sync_id")]
    public Guid SyncId { get; set; }

    [Column("promotion_name")]
    public string PromotionName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("start_date")]
    public DateTime? StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at_utc")]
    public DateTime? DeletedAtUtc { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAtUtc { get; set; }
}
