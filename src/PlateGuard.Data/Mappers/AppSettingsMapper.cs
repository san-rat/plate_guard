using PlateGuard.Core.Models;
using PlateGuard.Data.Entities;

namespace PlateGuard.Data.Mappers;

internal static class AppSettingsMapper
{
    public static AppSettings ToModel(SettingsEntity entity)
    {
        return new AppSettings
        {
            Id = entity.Id,
            DeletePasswordHash = entity.DeletePasswordHash,
            ShopName = entity.ShopName,
            ExportFolder = entity.ExportFolder,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static SettingsEntity ToEntity(AppSettings model)
    {
        return new SettingsEntity
        {
            Id = model.Id,
            DeletePasswordHash = model.DeletePasswordHash ?? string.Empty,
            ShopName = model.ShopName,
            ExportFolder = model.ExportFolder,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }

    public static void UpdateEntity(SettingsEntity entity, AppSettings model)
    {
        entity.DeletePasswordHash = model.DeletePasswordHash ?? string.Empty;
        entity.ShopName = model.ShopName;
        entity.ExportFolder = model.ExportFolder;
        entity.UpdatedAt = model.UpdatedAt;
    }
}
