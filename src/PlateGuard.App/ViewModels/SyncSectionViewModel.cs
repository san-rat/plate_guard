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
    private string lastResultText = "No sync attempt yet.";

    [ObservableProperty]
    private string? lastErrorMessage;

    [ObservableProperty]
    private bool hasLastError;

    [ObservableProperty]
    private int unacknowledgedConflictCount;

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
            _ = RefreshConflictsAsync();
            return;
        }

        Dispatcher.UIThread.Post(async () =>
        {
            ApplySchedulerState(state);
            await RefreshConflictsAsync();
        });
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

        if (!IsConfigured)
        {
            SyncStatusText = "Cloud sync is unconfigured";
            LastResultText = "Cloud sync is unavailable until it is configured.";
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
