namespace PlateGuard.Cloud;

public interface ISyncEngine
{
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);
}
