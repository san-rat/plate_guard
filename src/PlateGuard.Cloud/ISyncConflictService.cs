using Microsoft.EntityFrameworkCore;
using PlateGuard.Data.Db;

namespace PlateGuard.Cloud;

public interface ISyncConflictService
{
    Task<IReadOnlyList<SyncConflictItem>> ListUnacknowledgedAsync(CancellationToken cancellationToken = default);
    Task<int> CountUnacknowledgedAsync(CancellationToken cancellationToken = default);
    Task AcknowledgeAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class SyncConflictService(PlateGuardDbContextFactory dbContextFactory) : ISyncConflictService
{
    private readonly PlateGuardDbContextFactory _dbContextFactory = dbContextFactory;

    public async Task<IReadOnlyList<SyncConflictItem>> ListUnacknowledgedAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext([]);
        return await dbContext.SyncConflicts
            .AsNoTracking()
            .Where(conflict => !conflict.IsAcknowledged)
            .OrderByDescending(conflict => conflict.DetectedAtUtc)
            .Select(conflict => new SyncConflictItem
            {
                Id = conflict.Id,
                Kind = conflict.Kind,
                VehicleNumberRaw = conflict.VehicleNumberRaw,
                PromotionName = conflict.PromotionName,
                ServiceDate = conflict.ServiceDate,
                Details = conflict.Details,
                DetectedAtUtc = conflict.DetectedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUnacknowledgedAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext([]);
        return await dbContext.SyncConflicts.CountAsync(conflict => !conflict.IsAcknowledged, cancellationToken);
    }

    public async Task AcknowledgeAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext([]);
        var conflict = await dbContext.SyncConflicts.SingleOrDefaultAsync(conflict => conflict.Id == id, cancellationToken);
        if (conflict is null)
        {
            return;
        }

        conflict.IsAcknowledged = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
