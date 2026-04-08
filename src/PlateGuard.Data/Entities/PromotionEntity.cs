namespace PlateGuard.Data.Entities;

public sealed class PromotionEntity
{
    public int Id { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<PromotionUsageEntity> PromotionUsages { get; set; } = [];
}
