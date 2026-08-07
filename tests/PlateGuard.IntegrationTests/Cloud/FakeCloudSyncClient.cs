using PlateGuard.Cloud;
using PlateGuard.Cloud.Dtos;

namespace PlateGuard.IntegrationTests.Cloud;

internal sealed class FakeCloudSyncClient : ICloudSyncClient
{
    public List<VehicleRow> VehicleRows { get; } = [];
    public List<PromotionRow> PromotionRows { get; } = [];
    public List<PromotionUsageRow> PromotionUsageRows { get; } = [];
    public List<VehicleRow> ReceivedVehicles { get; } = [];
    public List<PromotionRow> ReceivedPromotions { get; } = [];
    public List<PromotionUsageRow> ReceivedPromotionUsages { get; } = [];
    public HashSet<Guid> RejectedVehicleSyncIds { get; } = [];
    public HashSet<Guid> RejectedPromotionUsageSyncIds { get; } = [];
    public int SignInCalls { get; private set; }
    public int FetchCalls { get; private set; }
    public int VehicleUpsertCalls { get; private set; }
    public int PromotionUsageUpsertCalls { get; private set; }

    public int TotalCalls => SignInCalls + FetchCalls + ReceivedVehicles.Count + ReceivedPromotions.Count + ReceivedPromotionUsages.Count;

    public Task SignInAsync(CancellationToken cancellationToken = default)
    {
        SignInCalls++;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VehicleRow>> FetchVehiclesAsync(DateTime? changedAfterUtc, CancellationToken cancellationToken = default)
    {
        FetchCalls++;
        return Task.FromResult<IReadOnlyList<VehicleRow>>(FilterChanged(VehicleRows, changedAfterUtc));
    }

    public Task<IReadOnlyList<PromotionRow>> FetchPromotionsAsync(DateTime? changedAfterUtc, CancellationToken cancellationToken = default)
    {
        FetchCalls++;
        return Task.FromResult<IReadOnlyList<PromotionRow>>(FilterChanged(PromotionRows, changedAfterUtc));
    }

    public Task<IReadOnlyList<PromotionUsageRow>> FetchPromotionUsagesAsync(DateTime? changedAfterUtc, CancellationToken cancellationToken = default)
    {
        FetchCalls++;
        return Task.FromResult<IReadOnlyList<PromotionUsageRow>>(FilterChanged(PromotionUsageRows, changedAfterUtc));
    }

    public Task<VehicleRow?> FindLiveVehicleByNormalizedNumberAsync(string vehicleNumberNormalized, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(VehicleRows.SingleOrDefault(row => !row.IsDeleted && row.VehicleNumberNormalized == vehicleNumberNormalized));
    }

    public Task<PromotionUsageRow?> FindLivePromotionUsageAsync(Guid vehicleSyncId, Guid promotionSyncId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PromotionUsageRows.SingleOrDefault(row => !row.IsDeleted && row.VehicleSyncId == vehicleSyncId && row.PromotionSyncId == promotionSyncId));
    }

    public Task<CloudUpsertResult> UpsertVehiclesAsync(IReadOnlyCollection<VehicleRow> rows, CancellationToken cancellationToken = default)
    {
        VehicleUpsertCalls++;
        ReceivedVehicles.AddRange(rows);
        var rejected = rows.FirstOrDefault(row => RejectedVehicleSyncIds.Contains(row.SyncId));
        if (rejected is not null)
        {
            throw new CloudUniqueConstraintException(rejected.SyncId, new InvalidOperationException("Unique violation."));
        }

        return Task.FromResult(Accept(rows, row => row.SyncId, row => row.UpdatedAtUtc));
    }

    public Task<CloudUpsertResult> UpsertPromotionsAsync(IReadOnlyCollection<PromotionRow> rows, CancellationToken cancellationToken = default)
    {
        ReceivedPromotions.AddRange(rows);
        return Task.FromResult(Accept(rows, row => row.SyncId, row => row.UpdatedAtUtc));
    }

    public Task<CloudUpsertResult> UpsertPromotionUsagesAsync(IReadOnlyCollection<PromotionUsageRow> rows, CancellationToken cancellationToken = default)
    {
        PromotionUsageUpsertCalls++;
        ReceivedPromotionUsages.AddRange(rows);
        var rejected = rows.FirstOrDefault(row => RejectedPromotionUsageSyncIds.Contains(row.SyncId));
        if (rejected is not null)
        {
            throw new CloudUniqueConstraintException(rejected.SyncId, new InvalidOperationException("Unique violation."));
        }

        return Task.FromResult(Accept(rows, row => row.SyncId, row => row.UpdatedAtUtc));
    }

    private static IReadOnlyList<T> FilterChanged<T>(IEnumerable<T> rows, DateTime? changedAfterUtc)
        where T : class
    {
        return rows.Where(row => GetUpdatedAtUtc(row) is DateTime updatedAtUtc && (!changedAfterUtc.HasValue || updatedAtUtc > changedAfterUtc.Value)).ToList();
    }

    private static DateTime? GetUpdatedAtUtc<T>(T row) where T : class
    {
        return row switch
        {
            VehicleRow vehicle => vehicle.UpdatedAtUtc,
            PromotionRow promotion => promotion.UpdatedAtUtc,
            PromotionUsageRow usage => usage.UpdatedAtUtc,
            _ => null
        };
    }

    private static CloudUpsertResult Accept<T>(IReadOnlyCollection<T> rows, Func<T, Guid> getSyncId, Func<T, DateTime?> getUpdatedAtUtc)
    {
        return new CloudUpsertResult(rows.Select(row => new CloudAcceptedRow(getSyncId(row), getUpdatedAtUtc(row) ?? DateTime.UtcNow)).ToList());
    }
}
