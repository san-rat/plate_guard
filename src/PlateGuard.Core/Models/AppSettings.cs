namespace PlateGuard.Core.Models;

public sealed class AppSettings
{
    public const int DefaultId = 1;

    public int Id { get; set; }
    public string? DeletePasswordHash { get; set; }
    public string? ShopName { get; set; }
    public string? ExportFolder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
