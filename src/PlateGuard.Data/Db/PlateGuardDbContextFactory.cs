using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PlateGuard.Data.Db;

public sealed class PlateGuardDbContextFactory : IDesignTimeDbContextFactory<PlateGuardDbContext>
{
    public PlateGuardDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlateGuardDbContext>();
        var databasePath = PlateGuardDatabasePathProvider.GetDatabasePath();

        optionsBuilder.UseSqlite($"Data Source={databasePath}");

        return new PlateGuardDbContext(optionsBuilder.Options);
    }
}
