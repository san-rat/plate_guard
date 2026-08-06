using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Models;
using PlateGuard.Data.Db;

namespace PlateGuard.Cloud;

public sealed class SyncScheduler(
    ISyncEngine syncEngine,
    PlateGuardDbContextFactory dbContextFactory,
    ISyncLog syncLog) : IAsyncDisposable
{
    private static readonly TimeSpan SyncPeriod = TimeSpan.FromHours(6);
    private readonly ISyncEngine _syncEngine = syncEngine;
    private readonly PlateGuardDbContextFactory _dbContextFactory = dbContextFactory;
    private readonly ISyncLog _syncLog = syncLog;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private Task? _runTask;
    private int _started;

    public SyncSchedulerState State { get; private set; } = new(false, null, null, null);

    public event Action<SyncSchedulerState>? StateChanged;

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        _runTask = Task.Run(RunAsync);
    }

    public async Task StopAsync()
    {
        _cancellationTokenSource.Cancel();
        if (_runTask is not null)
        {
            await Task.WhenAny(_runTask, Task.Delay(TimeSpan.FromSeconds(5)));
        }
    }

    public Task SyncNowAsync(CancellationToken cancellationToken = default)
    {
        return SyncOnceAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _syncGate.Dispose();
        _cancellationTokenSource.Dispose();
    }

    private async Task RunAsync()
    {
        try
        {
            if (await IsCatchUpRequiredAsync(_cancellationTokenSource.Token))
            {
                await SyncOnceAsync(_cancellationTokenSource.Token);
            }

            using var timer = new PeriodicTimer(SyncPeriod);
            while (await timer.WaitForNextTickAsync(_cancellationTokenSource.Token))
            {
                await SyncOnceAsync(_cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _syncLog.Info($"Cloud sync scheduler stopped: {exception.Message}");
        }
    }

    private async Task<bool> IsCatchUpRequiredAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext([]);
        var settings = await dbContext.Settings.SingleOrDefaultAsync(settings => settings.Id == AppSettings.DefaultId, cancellationToken);
        return settings?.LastSyncedAtUtc is null || DateTime.UtcNow - settings.LastSyncedAtUtc.Value >= SyncPeriod;
    }

    private async Task SyncOnceAsync(CancellationToken cancellationToken)
    {
        if (!await _syncGate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            UpdateState(State with { IsRunning = true, LastErrorMessage = null });

            var result = await _syncEngine.SyncAsync(cancellationToken);
            UpdateState(new SyncSchedulerState(
                false,
                result.Status == SyncStatus.Success ? DateTime.UtcNow : State.LastSyncedAtUtc,
                result,
                result.ErrorMessage));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            UpdateState(State with { IsRunning = false });
            throw;
        }
        catch (Exception exception)
        {
            UpdateState(new SyncSchedulerState(false, State.LastSyncedAtUtc, null, exception.Message));
            throw;
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private void UpdateState(SyncSchedulerState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }
}

public sealed record SyncSchedulerState(
    bool IsRunning,
    DateTime? LastSyncedAtUtc,
    SyncResult? LastResult,
    string? LastErrorMessage);
