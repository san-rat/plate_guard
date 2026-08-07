using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlateGuard.Cloud;

namespace PlateGuard.App.ViewModels;

public sealed partial class SyncSectionViewModel : ViewModelBase
{
    private readonly CloudSyncOptions? _options;
    private readonly SyncScheduler? _syncScheduler;
    private readonly ISyncConflictService? _syncConflictService;

    public SyncSectionViewModel()
    {
        IsConfigured = true;
        SyncStatusText = "Cloud sync is ready";
        LastSyncedText = "Today at 10:30";
        LastResultText = "Last sync completed successfully.";
        UnacknowledgedConflictCount = 1;
        HasUnacknowledgedConflicts = true;
        HasNoUnacknowledgedConflicts = false;
    }

    public SyncSectionViewModel(
        CloudSyncOptions options,
        SyncScheduler syncScheduler,
        ISyncConflictService syncConflictService)
    {
        _options = options;
        _syncScheduler = syncScheduler;
        _syncConflictService = syncConflictService;
        IsConfigured = options.IsConfigured;
        ApplySchedulerState(syncScheduler.State);
        syncScheduler.StateChanged += OnSchedulerStateChanged;
    }

    public ObservableCollection<SyncConflictItemViewModel> Conflicts { get; } = [];

    [ObservableProperty]
    private bool isConfigured;

    [ObservableProperty]
    private bool isSyncRunning;

    [ObservableProperty]
    private string syncStatusText = "Cloud sync is unconfigured";

    [ObservableProperty]
    private string lastSyncedText = "Never";

    [ObservableProperty]
    private string nextSyncText = "-";

    [ObservableProperty]
    private string lastResultText = "No sync attempt yet.";

    [ObservableProperty]
    private string? lastErrorMessage;

    [ObservableProperty]
    private bool hasLastError;

    [ObservableProperty]
    private int unacknowledgedConflictCount;

    public string UnacknowledgedConflictText => UnacknowledgedConflictCount == 1
        ? "1 unacknowledged conflict"
        : $"{UnacknowledgedConflictCount} unacknowledged conflicts";

    partial void OnUnacknowledgedConflictCountChanged(int value)
    {
        OnPropertyChanged(nameof(UnacknowledgedConflictText));
    }

    [ObservableProperty]
    private bool hasUnacknowledgedConflicts;

    [ObservableProperty]
    private bool hasNoUnacknowledgedConflicts = true;

    public bool CanSyncNow => IsConfigured && !IsSyncRunning;

    partial void OnIsConfiguredChanged(bool value)
    {
        SyncNowCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSyncRunningChanged(bool value)
    {
        SyncNowCommand.NotifyCanExecuteChanged();
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_options is null || _syncScheduler is null || _syncConflictService is null)
        {
            return;
        }

        IsConfigured = _options.IsConfigured;
        ApplySchedulerState(_syncScheduler.State);
        await RefreshConflictsAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanSyncNow))]
    private async Task SyncNowAsync()
    {
        if (_syncScheduler is null)
        {
            return;
        }

        await _syncScheduler.SyncNowAsync();
        ApplySchedulerState(_syncScheduler.State);
        await RefreshConflictsAsync();
    }

    private void OnSchedulerStateChanged(SyncSchedulerState state)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplySchedulerState(state);
            _ = RefreshConflictsSafelyAsync();
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            ApplySchedulerState(state);
            _ = RefreshConflictsSafelyAsync();
        });
    }

    // "Last synced: Never" alone leaves the operator unable to tell whether sync is working at
    // all, so the six-hourly cadence is stated explicitly rather than left implicit.
    private string DescribeNextSync(DateTime? lastSyncedAtUtc)
    {
        if (!IsConfigured)
        {
            return "Not scheduled";
        }

        if (lastSyncedAtUtc is null)
        {
            return "As soon as the app can reach the cloud";
        }

        var dueAtUtc = lastSyncedAtUtc.Value + SyncScheduler.SyncPeriod;
        return dueAtUtc <= DateTime.UtcNow
            ? "Due now"
            : dueAtUtc.ToLocalTime().ToString("g");
    }

    // Raised from the sync background thread while the engine is writing to the same SQLite
    // file, so this can genuinely fail. An unobserved throw here would land on the dispatcher.
    private async Task RefreshConflictsSafelyAsync()
    {
        try
        {
            await RefreshConflictsAsync();
        }
        catch
        {
            // Counts refresh again on the next sync or when the Sync section is opened.
        }
    }

    private async Task RefreshConflictsAsync(CancellationToken cancellationToken = default)
    {
        if (_syncConflictService is null)
        {
            return;
        }

        var conflicts = await _syncConflictService.ListUnacknowledgedAsync(cancellationToken);
        Conflicts.Clear();
        foreach (var conflict in conflicts)
        {
            Conflicts.Add(new SyncConflictItemViewModel(conflict, AcknowledgeAsync));
        }

        UnacknowledgedConflictCount = await _syncConflictService.CountUnacknowledgedAsync(cancellationToken);
        HasUnacknowledgedConflicts = UnacknowledgedConflictCount > 0;
        HasNoUnacknowledgedConflicts = !HasUnacknowledgedConflicts;
    }

    private async Task AcknowledgeAsync(int id)
    {
        if (_syncConflictService is null)
        {
            return;
        }

        await _syncConflictService.AcknowledgeAsync(id);
        await RefreshConflictsAsync();
    }

    private void ApplySchedulerState(SyncSchedulerState state)
    {
        IsSyncRunning = state.IsRunning;
        LastSyncedText = state.LastSyncedAtUtc?.ToLocalTime().ToString("g") ?? "Never";
        LastErrorMessage = state.LastErrorMessage;
        HasLastError = !string.IsNullOrWhiteSpace(LastErrorMessage);
        NextSyncText = DescribeNextSync(state.LastSyncedAtUtc);

        if (!IsConfigured)
        {
            SyncStatusText = "Cloud sync is unconfigured";
            // A broken settings file is the one unconfigured case an operator can act on, so it
            // is reported instead of the generic "not set up yet" guidance.
            LastResultText = _options?.ConfigurationError is { } configurationError
                ? configurationError
                : $"Cloud sync settings are missing. Reinstall PlateGuard using the setup file supplied for this shop, or restore {CloudSyncOptions.GetConfigurationFilePath()}.";
            return;
        }

        if (state.IsRunning)
        {
            SyncStatusText = "Cloud sync is running";
            LastResultText = "Syncing changes with the cloud.";
            return;
        }

        switch (state.LastResult?.Status)
        {
            case SyncStatus.Success:
                SyncStatusText = "Cloud sync completed";
                LastResultText = "Last sync completed successfully.";
                break;
            case SyncStatus.Failed:
                SyncStatusText = "Cloud sync failed";
                LastResultText = "The last sync could not be completed.";
                break;
            case SyncStatus.Unconfigured:
                SyncStatusText = "Cloud sync is unconfigured";
                LastResultText = "Cloud sync is unavailable until it is configured.";
                break;
            default:
                SyncStatusText = "Cloud sync is ready";
                LastResultText = "No sync attempt yet.";
                break;
        }
    }
}
