using PlateGuard.Core.Models;
using PlateGuard.Data.Entities;

namespace PlateGuard.Data.Mappers;

internal static class PromotionMapper
{
    public static Promotion ToModel(PromotionEntity entity)
    {
        return new Promotion
        {
            Id = entity.Id,
            PromotionName = entity.PromotionName,
            Description = entity.Description,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static PromotionEntity ToEntity(Promotion model)
    {
        return new PromotionEntity
        {
            Id = model.Id,
            PromotionName = model.PromotionName,
            Description = model.Description,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    public static void UpdateEntity(PromotionEntity entity, Promotion model)
    {
        entity.PromotionName = model.PromotionName;
        entity.Description = model.Description;
        entity.StartDate = model.StartDate;
        entity.EndDate = model.EndDate;
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = model.UpdatedAt;
    }
}
