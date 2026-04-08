using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Data.Db;
using PlateGuard.Data.Mappers;

namespace PlateGuard.Data.Repositories;

public sealed class SettingsRepository() : RepositoryBase(new PlateGuardDbContextFactory()), ISettingsRepository
{
    public async Task<AppSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        var entity = await dbContext.Settings
            .AsNoTracking()
            .OrderBy(settings => settings.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : AppSettingsMapper.ToModel(entity);
    }

    public async Task<AppSettings> UpsertAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var settingsId = settings.Id == 0 ? PlateGuardDatabaseInitializer.DefaultSettingsId : settings.Id;
        var existingEntity = await dbContext.Settings.FirstOrDefaultAsync(entity => entity.Id == settingsId, cancellationToken);

        if (existingEntity is null)
        {
            var newEntity = AppSettingsMapper.ToEntity(settings);
            newEntity.Id = settingsId;
            newEntity.CreatedAt = newEntity.CreatedAt == default ? DateTime.UtcNow : newEntity.CreatedAt;

            await dbContext.Settings.AddAsync(newEntity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return AppSettingsMapper.ToModel(newEntity);
        }

        settings.UpdatedAt = DateTime.UtcNow;
        AppSettingsMapper.UpdateEntity(existingEntity, settings);

        await dbContext.SaveChangesAsync(cancellationToken);

        return AppSettingsMapper.ToModel(existingEntity);
    }
}
