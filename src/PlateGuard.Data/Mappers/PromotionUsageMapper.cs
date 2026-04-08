using PlateGuard.Core.Models;
using PlateGuard.Data.Entities;

namespace PlateGuard.Data.Mappers;

internal static class PromotionUsageMapper
{
    public static PromotionUsage ToModel(PromotionUsageEntity entity)
    {
        return new PromotionUsage
        {
            Id = entity.Id,
            VehicleId = entity.VehicleId,
            PromotionId = entity.PromotionId,
            ServiceDate = entity.ServiceDate,
            Mileage = entity.Mileage,
            NormalPrice = entity.NormalPrice,
            DiscountedPrice = entity.DiscountedPrice,
            AmountPaid = entity.AmountPaid,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static PromotionUsageEntity ToEntity(PromotionUsage model)
    {
        return new PromotionUsageEntity
        {
            Id = model.Id,
            VehicleId = model.VehicleId,
            PromotionId = model.PromotionId,
            ServiceDate = model.ServiceDate,
            Mileage = model.Mileage,
            NormalPrice = model.NormalPrice,
            DiscountedPrice = model.DiscountedPrice,
            AmountPaid = model.AmountPaid,
            Notes = model.Notes,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    public static void UpdateEntity(PromotionUsageEntity entity, PromotionUsage model)
    {
        entity.VehicleId = model.VehicleId;
        entity.PromotionId = model.PromotionId;
        entity.ServiceDate = model.ServiceDate;
        entity.Mileage = model.Mileage;
        entity.NormalPrice = model.NormalPrice;
        entity.DiscountedPrice = model.DiscountedPrice;
        entity.AmountPaid = model.AmountPaid;
        entity.Notes = model.Notes;
        entity.UpdatedAt = model.UpdatedAt;
    }
}
