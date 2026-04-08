using PlateGuard.Data.Db;

namespace PlateGuard.Data.Repositories;

public abstract class RepositoryBase(PlateGuardDbContextFactory dbContextFactory)
{
    private readonly PlateGuardDbContextFactory _dbContextFactory = dbContextFactory;

    protected PlateGuardDbContext CreateDbContext()
    {
        return _dbContextFactory.CreateDbContext([]);
    }
}
