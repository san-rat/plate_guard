using PlateGuard.Cloud.Dtos;

namespace PlateGuard.Cloud;

public interface ICloudSyncClient
{
    Task SignInAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VehicleRow>> FetchVehiclesAsync(DateTime? changedAfterUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromotionRow>> FetchPromotionsAsync(DateTime? changedAfterUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PromotionUsageRow>> FetchPromotionUsagesAsync(DateTime? changedAfterUtc, CancellationToken cancellationToken = default);
    Task<CloudUpsertResult> UpsertVehiclesAsync(IReadOnlyCollection<VehicleRow> rows, CancellationToken cancellationToken = default);
    Task<CloudUpsertResult> UpsertPromotionsAsync(IReadOnlyCollection<PromotionRow> rows, CancellationToken cancellationToken = default);
    Task<CloudUpsertResult> UpsertPromotionUsagesAsync(IReadOnlyCollection<PromotionUsageRow> rows, CancellationToken cancellationToken = default);
}

public sealed class CloudUpsertResult(IReadOnlyList<CloudAcceptedRow> acceptedRows)
{
    public IReadOnlyList<CloudAcceptedRow> AcceptedRows { get; } = acceptedRows;
}

public sealed class CloudAcceptedRow(Guid syncId, DateTime? updatedAtUtc)
{
    public Guid SyncId { get; } = syncId;
    public DateTime? UpdatedAtUtc { get; } = updatedAtUtc;
}

public sealed class CloudUniqueConstraintException(Guid syncId, Exception innerException)
    : Exception($"Cloud unique constraint rejected sync id {syncId}.", innerException)
{
    public Guid SyncId { get; } = syncId;
}
