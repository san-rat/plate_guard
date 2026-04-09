using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Helpers;
using PlateGuard.Core.Models;
using PlateGuard.Data.Entities;

namespace PlateGuard.Data.Db;

public sealed class PlateGuardDatabaseInitializer(PlateGuardDbContextFactory dbContextFactory)
{
    private const string DefaultDeletePassword = "admin";
    private readonly PlateGuardDbContextFactory _dbContextFactory = dbContextFactory;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var databasePath = PlateGuardDatabasePathProvider.GetDatabasePath();
        var databaseFolder = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(databaseFolder))
        {
            Directory.CreateDirectory(databaseFolder);
        }

        await using var dbContext = _dbContextFactory.CreateDbContext([]);

        await dbContext.Database.MigrateAsync(cancellationToken);

        var settingsExist = await dbContext.Settings.AnyAsync(settings => settings.Id == AppSettings.DefaultId, cancellationToken);
        if (settingsExist)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;
        var settings = new SettingsEntity
        {
            Id = AppSettings.DefaultId,
            DeletePasswordHash = DeletePasswordHasher.Hash(DefaultDeletePassword),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        await dbContext.Settings.AddAsync(settings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
