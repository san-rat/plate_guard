using PlateGuard.Cloud.Dtos;
using Supabase.Postgrest;
using Supabase.Postgrest.Exceptions;
using Supabase.Postgrest.Interfaces;
using Supabase.Postgrest.Models;

namespace PlateGuard.Cloud;

public sealed class SupabaseCloudSyncClient(CloudSyncOptions options) : ICloudSyncClient
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
    private readonly CloudSyncOptions _options = options;
    private Supabase.Client? _client;
    private bool _initialized;

    public async Task SignInAsync(CancellationToken cancellationToken = default)
    {
        using var timeoutCancellationTokenSource = CreateTimeoutCancellationTokenSource(cancellationToken);
        var requestCancellationToken = timeoutCancellationTokenSource.Token;
        requestCancellationToken.ThrowIfCancellationRequested();

        var client = GetClient();
        if (!_initialized)
        {
            await client.InitializeAsync().WaitAsync(requestCancellationToken);
            _initialized = true;
        }

        // Sign in on every sync rather than caching the session: the access token expires long
        // before the next six-hourly tick, so a cached one would 401 on every run after the first.
        await client.Auth.SignInWithPassword(_options.ServiceAccountEmail, _options.ServiceAccountPassword).WaitAsync(requestCancellationToken);
    }

    public Task<IReadOnlyList<VehicleRow>> FetchVehiclesAsync(DateTime? changedAfterUtc, CancellationToken cancellationToken = default)
    {
        return FetchAsync<VehicleRow>(changedAfterUtc, cancellationToken);
    }

    public Task<IReadOnlyList<PromotionRow>> FetchPromotionsAsync(DateTime? changedAfterUtc, CancellationToken cancellationToken = default)
    {
        return FetchAsync<PromotionRow>(changedAfterUtc, cancellationToken);
    }

    public Task<IReadOnlyList<PromotionUsageRow>> FetchPromotionUsagesAsync(DateTime? changedAfterUtc, CancellationToken cancellationToken = default)
    {
        return FetchAsync<PromotionUsageRow>(changedAfterUtc, cancellationToken);
    }

    public Task<CloudUpsertResult> UpsertVehiclesAsync(IReadOnlyCollection<VehicleRow> rows, CancellationToken cancellationToken = default)
    {
        return UpsertAsync(rows, row => row.SyncId, row => row.UpdatedAtUtc, cancellationToken);
    }

    public Task<CloudUpsertResult> UpsertPromotionsAsync(IReadOnlyCollection<PromotionRow> rows, CancellationToken cancellationToken = default)
    {
        return UpsertAsync(rows, row => row.SyncId, row => row.UpdatedAtUtc, cancellationToken);
    }

    public Task<CloudUpsertResult> UpsertPromotionUsagesAsync(IReadOnlyCollection<PromotionUsageRow> rows, CancellationToken cancellationToken = default)
    {
        return UpsertAsync(rows, row => row.SyncId, row => row.UpdatedAtUtc, cancellationToken);
    }

    private Supabase.Client GetClient()
    {
        return _client ??= new Supabase.Client(
            _options.SupabaseUrl,
            _options.SupabaseAnonKey,
            new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false,
                AutoRefreshToken = true
            });
    }

    private async Task<IReadOnlyList<T>> FetchAsync<T>(DateTime? changedAfterUtc, CancellationToken cancellationToken)
        where T : BaseModel, new()
    {
        using var timeoutCancellationTokenSource = CreateTimeoutCancellationTokenSource(cancellationToken);
        var requestCancellationToken = timeoutCancellationTokenSource.Token;
        requestCancellationToken.ThrowIfCancellationRequested();
        IPostgrestTable<T> table = GetClient().From<T>();
        if (changedAfterUtc.HasValue)
        {
            table = table.Filter("updated_at", Constants.Operator.GreaterThan, changedAfterUtc.Value.ToUniversalTime());
        }

        var response = await table.Get(requestCancellationToken);
        return response.Models;
    }

    private async Task<CloudUpsertResult> UpsertAsync<T>(
        IReadOnlyCollection<T> rows,
        Func<T, Guid> getSyncId,
        Func<T, DateTime?> getUpdatedAtUtc,
        CancellationToken cancellationToken)
        where T : BaseModel, new()
    {
        if (rows.Count == 0)
        {
            return new CloudUpsertResult([]);
        }

        try
        {
            using var timeoutCancellationTokenSource = CreateTimeoutCancellationTokenSource(cancellationToken);
            var response = await GetClient().From<T>().Upsert(
                rows.ToList(),
                new QueryOptions { OnConflict = "sync_id" },
                timeoutCancellationTokenSource.Token);

            return new CloudUpsertResult(response.Models
                .Select(row => new CloudAcceptedRow(getSyncId(row), getUpdatedAtUtc(row)))
                .ToList());
        }
        catch (PostgrestException exception) when (IsUniqueConstraintViolation(exception) && rows.Count == 1)
        {
            throw new CloudUniqueConstraintException(getSyncId(rows.Single()), exception);
        }
    }

    private static bool IsUniqueConstraintViolation(PostgrestException exception)
    {
        var details = $"{exception.Message} {exception.Content} {exception.Reason}";
        return exception.StatusCode == 409 ||
            details.Contains("23505", StringComparison.Ordinal) ||
            details.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
            details.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    private static CancellationTokenSource CreateTimeoutCancellationTokenSource(CancellationToken cancellationToken)
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(RequestTimeout);
        return cancellationTokenSource;
    }
}
