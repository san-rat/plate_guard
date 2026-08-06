using Microsoft.EntityFrameworkCore;
using PlateGuard.App.ViewModels;
using PlateGuard.Cloud;
using PlateGuard.Core.Interfaces;
using PlateGuard.Data.Db;
using PlateGuard.Data.Entities;
using PlateGuard.IntegrationTests.Cloud;
using PlateGuard.IntegrationTests.Infrastructure;

namespace PlateGuard.IntegrationTests.ViewModels;

public sealed class SyncSectionViewModelIntegrationTests
{
    [Fact]
    public async Task UnconfiguredState_ReportsUnconfiguredAndDisablesSyncNow()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var viewModel = CreateSyncSection(app, new CloudSyncOptions());

        await viewModel.LoadAsync();

        Assert.False(viewModel.IsConfigured);
        Assert.Equal("Cloud sync is unconfigured", viewModel.SyncStatusText);
        Assert.False(viewModel.SyncNowCommand.CanExecute(null));
    }

    [Fact]
    public async Task ConflictsAreListed_WithPlatePromotionAndServiceDate()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        await SeedConflictAsync(app, "DuplicateRedemption", "CFL-1001", "August offer", new DateTime(2026, 8, 5), detectedOffset: 1);
        await SeedConflictAsync(app, "DuplicateRedemption", "CFL-1002", "September offer", new DateTime(2026, 8, 6), detectedOffset: 2);
        var viewModel = CreateSyncSection(app, new CloudSyncOptions());

        await viewModel.LoadAsync();

        Assert.Equal(2, viewModel.Conflicts.Count);
        Assert.All(viewModel.Conflicts, conflict =>
        {
            Assert.False(string.IsNullOrWhiteSpace(conflict.Plate));
            Assert.False(string.IsNullOrWhiteSpace(conflict.PromotionName));
            Assert.NotNull(conflict.ServiceDate);
        });
    }

    [Fact]
    public async Task AcknowledgingConflict_RemovesItAndMarksTheDatabaseRow()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var acknowledgedId = await SeedConflictAsync(app, "DuplicateRedemption", "ACK-1001", "Acknowledge offer", new DateTime(2026, 8, 5), detectedOffset: 1);
        var remainingId = await SeedConflictAsync(app, "DuplicateRedemption", "ACK-1002", "Remaining offer", new DateTime(2026, 8, 6), detectedOffset: 2);
        var viewModel = CreateSyncSection(app, new CloudSyncOptions());
        await viewModel.LoadAsync();

        await viewModel.Conflicts.Single(conflict => conflict.Id == acknowledgedId).AcknowledgeCommand.ExecuteAsync(null);

        Assert.DoesNotContain(viewModel.Conflicts, conflict => conflict.Id == acknowledgedId);
        Assert.Contains(viewModel.Conflicts, conflict => conflict.Id == remainingId);
        await using var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        Assert.True((await dbContext.SyncConflicts.SingleAsync(conflict => conflict.Id == acknowledgedId)).IsAcknowledged);
    }

    [Fact]
    public async Task ConflictCount_OnlyIncludesUnacknowledgedRows()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        await SeedConflictAsync(app, "DuplicateRedemption", "CNT-1001", "Open offer", new DateTime(2026, 8, 5), detectedOffset: 1);
        var acknowledgedId = await SeedConflictAsync(app, "DuplicateRedemption", "CNT-1002", "Closed offer", new DateTime(2026, 8, 6), detectedOffset: 2);
        await using (var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]))
        {
            (await dbContext.SyncConflicts.SingleAsync(conflict => conflict.Id == acknowledgedId)).IsAcknowledged = true;
            await dbContext.SaveChangesAsync();
        }

        var viewModel = CreateSyncSection(app, new CloudSyncOptions());
        await viewModel.LoadAsync();

        Assert.Equal(1, viewModel.UnacknowledgedConflictCount);
        Assert.Single(viewModel.Conflicts);
        Assert.True(viewModel.HasUnacknowledgedConflicts);
    }

    [Fact]
    public async Task SectionSwitching_ShowsOnlyTheSelectedSyncSection()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var sync = CreateSyncSection(app, new CloudSyncOptions());
        var viewModel = new MainWindowViewModel(
            app.GetRequiredService<IVehicleService>(),
            app.GetRequiredService<IPromotionService>(),
            app.GetRequiredService<IPromotionUsageService>(),
            app.GetRequiredService<ISettingsService>(),
            app.GetRequiredService<IExportService>(),
            sync);

        await viewModel.ShowSyncSectionCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsSyncSectionVisible);
        Assert.False(viewModel.IsSearchSectionVisible);
        Assert.False(viewModel.IsPromotionsSectionVisible);
        Assert.False(viewModel.IsHistorySectionVisible);
        Assert.False(viewModel.IsSettingsSectionVisible);

        viewModel.ShowSearchSectionCommand.Execute(null);
        Assert.False(viewModel.IsSyncSectionVisible);

        await viewModel.ShowPromotionsSectionCommand.ExecuteAsync(null);
        Assert.False(viewModel.IsSyncSectionVisible);

        await viewModel.ShowHistorySectionCommand.ExecuteAsync(null);
        Assert.False(viewModel.IsSyncSectionVisible);

        await viewModel.ShowSettingsSectionCommand.ExecuteAsync(null);
        Assert.False(viewModel.IsSyncSectionVisible);
    }

    [Fact]
    public async Task ManualSync_UpdatesLastSyncedAndLastResultState()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var options = ConfiguredOptions();
        var viewModel = CreateSyncSection(app, options, new FakeCloudSyncClient());
        await viewModel.LoadAsync();

        await viewModel.SyncNowCommand.ExecuteAsync(null);

        Assert.NotEqual("Never", viewModel.LastSyncedText);
        Assert.Equal("Last sync completed successfully.", viewModel.LastResultText);
        Assert.Equal("Cloud sync completed", viewModel.SyncStatusText);
    }

    private static SyncSectionViewModel CreateSyncSection(
        IntegrationTestApp app,
        CloudSyncOptions options,
        FakeCloudSyncClient? cloudSyncClient = null)
    {
        var engine = new SyncEngine(
            cloudSyncClient ?? new FakeCloudSyncClient(),
            app.GetRequiredService<PlateGuardDbContextFactory>(),
            options,
            new TestSyncLog());
        var scheduler = new SyncScheduler(engine, app.GetRequiredService<PlateGuardDbContextFactory>(), new TestSyncLog());
        var conflicts = new SyncConflictService(app.GetRequiredService<PlateGuardDbContextFactory>());
        return new SyncSectionViewModel(options, scheduler, conflicts);
    }

    private static CloudSyncOptions ConfiguredOptions() => new()
    {
        SupabaseUrl = "configured",
        SupabaseAnonKey = "configured",
        ServiceAccountEmail = "configured",
        ServiceAccountPassword = "configured"
    };

    private static async Task<int> SeedConflictAsync(
        IntegrationTestApp app,
        string kind,
        string plate,
        string promotion,
        DateTime serviceDate,
        int detectedOffset)
    {
        await using var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        var conflict = new SyncConflictEntity
        {
            Kind = kind,
            LocalSyncId = Guid.NewGuid(),
            VehicleNumberRaw = plate,
            PromotionName = promotion,
            ServiceDate = serviceDate,
            Details = "The cloud record was retained and the local record was dropped.",
            DetectedAtUtc = new DateTime(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc).AddMinutes(detectedOffset)
        };
        dbContext.SyncConflicts.Add(conflict);
        await dbContext.SaveChangesAsync();
        return conflict.Id;
    }

    private sealed class TestSyncLog : ISyncLog
    {
        public void Info(string message)
        {
        }
    }
}
