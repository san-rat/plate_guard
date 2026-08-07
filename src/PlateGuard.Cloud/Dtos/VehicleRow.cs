using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace PlateGuard.Cloud.Dtos;

[Table("vehicles")]
public sealed class VehicleRow : BaseModel
{
    [PrimaryKey("sync_id")]
    public Guid SyncId { get; set; }

    [Column("vehicle_number_raw")]
    public string VehicleNumberRaw { get; set; } = string.Empty;

    [Column("vehicle_number_normalized")]
    public string VehicleNumberNormalized { get; set; } = string.Empty;

    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Column("owner_name")]
    public string? OwnerName { get; set; }

    [Column("brand")]
    public string? Brand { get; set; }

    [Column("model")]
    public string? Model { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at_utc")]
    public DateTime? DeletedAtUtc { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAtUtc { get; set; }
}
