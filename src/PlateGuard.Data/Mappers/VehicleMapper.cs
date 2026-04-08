using PlateGuard.Core.Models;
using PlateGuard.Data.Entities;

namespace PlateGuard.Data.Mappers;

internal static class VehicleMapper
{
    public static Vehicle ToModel(VehicleEntity entity)
    {
        return new Vehicle
        {
            Id = entity.Id,
            VehicleNumberRaw = entity.VehicleNumberRaw,
            VehicleNumberNormalized = entity.VehicleNumberNormalized,
            PhoneNumber = entity.PhoneNumber,
            OwnerName = entity.OwnerName,
            Brand = entity.Brand,
            Model = entity.Model,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static VehicleEntity ToEntity(Vehicle model)
    {
        return new VehicleEntity
        {
            Id = model.Id,
            VehicleNumberRaw = model.VehicleNumberRaw,
            VehicleNumberNormalized = model.VehicleNumberNormalized,
            PhoneNumber = model.PhoneNumber,
            OwnerName = model.OwnerName,
            Brand = model.Brand,
            Model = model.Model,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    public static void UpdateEntity(VehicleEntity entity, Vehicle model)
    {
        entity.VehicleNumberRaw = model.VehicleNumberRaw;
        entity.VehicleNumberNormalized = model.VehicleNumberNormalized;
        entity.PhoneNumber = model.PhoneNumber;
        entity.OwnerName = model.OwnerName;
        entity.Brand = model.Brand;
        entity.Model = model.Model;
        entity.UpdatedAt = model.UpdatedAt;
    }
}
