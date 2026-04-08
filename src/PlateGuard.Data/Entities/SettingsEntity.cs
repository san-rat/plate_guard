namespace PlateGuard.Data.Entities;

public sealed class SettingsEntity
{
    public int Id { get; set; }
    public string DeletePasswordHash { get; set; } = string.Empty;
    public string? ShopName { get; set; }
    public string? ExportFolder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
