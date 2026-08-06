using Microsoft.EntityFrameworkCore;
using PlateGuard.Cloud;
using PlateGuard.Cloud.Dtos;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Data.Db;
using PlateGuard.IntegrationTests.Infrastructure;

namespace PlateGuard.IntegrationTests.Cloud;

public sealed class SyncEngineIntegrationTests
{
    [Fact]
    public async Task Push_SendsDirtyRowsWithCloudSyncIdsAndForeignKeys()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var promotion = await CreatePromotionAsync(app, "Push rows");
        var save = await SaveUsageAsync(app, promotion.Id, "PSH-1001", "0778100101");
        var fake = new FakeCloudSyncClient();

        var result = await CreateEngine(app, fake).SyncAsync();

        Assert.Equal(SyncStatus.Success, result.Status);
        Assert.Contains(fake.ReceivedVehicles, row => row.SyncId == save.Vehicle!.SyncId);
        Assert.Contains(fake.ReceivedPromotions, row => row.SyncId == promotion.SyncId);
        var usage = Assert.Single(fake.ReceivedPromotionUsages);
        Assert.Equal(save.PromotionUsage!.SyncId, usage.SyncId);
        Assert.Equal(save.Vehicle!.SyncId, usage.VehicleSyncId);
        Assert.Equal(promotion.SyncId, usage.PromotionSyncId);
    }

    [Fact]
    public async Task Push_ClearsIsDirtyAfterCloudAcceptance()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var promotion = await CreatePromotionAsync(app, "Clear dirty");
        var save = await SaveUsageAsync(app, promotion.Id, "CLD-1001", "0778100102");

        Assert.Equal(SyncStatus.Success, (await CreateEngine(app, new FakeCloudSyncClient()).SyncAsync()).Status);

        await using var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        Assert.False((await dbContext.Vehicles.IgnoreQueryFilters().SingleAsync(entity => entity.Id == save.Vehicle!.Id)).IsDirty);
        Assert.False((await dbContext.Promotions.IgnoreQueryFilters().SingleAsync(entity => entity.Id == promotion.Id)).IsDirty);
        Assert.False((await dbContext.PromotionUsages.IgnoreQueryFilters().SingleAsync(entity => entity.Id == save.PromotionUsage!.Id)).IsDirty);
    }

    [Fact]
    public async Task Push_SendsSoftDeletedUsageAsTombstone()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var promotion = await CreatePromotionAsync(app, "Tombstone");
        var save = await SaveUsageAsync(app, promotion.Id, "TMB-1001", "0778100103");
        var usageService = app.GetRequiredService<IPromotionUsageService>();
        Assert.True((await usageService.DeleteUsageAsync(save.PromotionUsage!.Id, "admin")).IsSuccess);
        var fake = new FakeCloudSyncClient();

        await CreateEngine(app, fake).SyncAsync();

        Assert.Contains(fake.ReceivedPromotionUsages, row => row.SyncId == save.PromotionUsage.SyncId && row.IsDeleted);
    }

    [Fact]
    public async Task Pull_InsertsUnknownVehicleAndPromotion()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var updatedAt = new DateTime(2026, 8, 6, 8, 0, 0, DateTimeKind.Utc);
        var vehicle = VehicleRow(Guid.NewGuid(), "PUL-1001", updatedAt);
        var promotion = PromotionRow(Guid.NewGuid(), "Pull promotion", updatedAt.AddMinutes(1));
        var fake = new FakeCloudSyncClient();
        fake.VehicleRows.Add(vehicle);
        fake.PromotionRows.Add(promotion);

        var result = await CreateEngine(app, fake).SyncAsync();

        Assert.Equal(1, result.VehiclesPulled);
        Assert.Equal(1, result.PromotionsPulled);
        await using var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        Assert.NotNull(await dbContext.Vehicles.IgnoreQueryFilters().SingleOrDefaultAsync(entity => entity.SyncId == vehicle.SyncId));
        Assert.NotNull(await dbContext.Promotions.IgnoreQueryFilters().SingleOrDefaultAsync(entity => entity.SyncId == promotion.SyncId));
    }

    [Fact]
    public async Task Pull_MapsUsageForeignKeysBySyncId()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var updatedAt = new DateTime(2026, 8, 6, 9, 0, 0, DateTimeKind.Utc);
        var vehicle = VehicleRow(Guid.NewGuid(), "FKM-1001", updatedAt);
        var promotion = PromotionRow(Guid.NewGuid(), "FK mapping", updatedAt);
        var usage = new PromotionUsageRow
        {
            SyncId = Guid.NewGuid(),
            VehicleSyncId = vehicle.SyncId,
            PromotionSyncId = promotion.SyncId,
            ServiceDate = updatedAt.Date,
            CreatedAt = updatedAt,
            UpdatedAtUtc = updatedAt.AddMinutes(1)
        };
        var fake = new FakeCloudSyncClient();
        fake.VehicleRows.Add(vehicle);
        fake.PromotionRows.Add(promotion);
        fake.PromotionUsageRows.Add(usage);

        await CreateEngine(app, fake).SyncAsync();

        await using var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        var localVehicle = await dbContext.Vehicles.IgnoreQueryFilters().SingleAsync(entity => entity.SyncId == vehicle.SyncId);
        var localPromotion = await dbContext.Promotions.IgnoreQueryFilters().SingleAsync(entity => entity.SyncId == promotion.SyncId);
        var localUsage = await dbContext.PromotionUsages.IgnoreQueryFilters().SingleAsync(entity => entity.SyncId == usage.SyncId);
        Assert.Equal(localVehicle.Id, localUsage.VehicleId);
        Assert.Equal(localPromotion.Id, localUsage.PromotionId);
    }

    [Fact]
    public async Task Pull_DefersUsageWithMissingParent()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var updatedAt = new DateTime(2026, 8, 6, 10, 0, 0, DateTimeKind.Utc);
        var fake = new FakeCloudSyncClient();
        fake.PromotionUsageRows.Add(new PromotionUsageRow
        {
            SyncId = Guid.NewGuid(),
            VehicleSyncId = Guid.NewGuid(),
            PromotionSyncId = Guid.NewGuid(),
            ServiceDate = updatedAt.Date,
            CreatedAt = updatedAt,
            UpdatedAtUtc = updatedAt
        });

        var result = await CreateEngine(app, fake).SyncAsync();

        Assert.Equal(1, result.Deferred);
        await using var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        Assert.Empty(await dbContext.PromotionUsages.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Pull_UsesLastWriteWinsAndKeepsNewerLocalRowDirty()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var vehicleService = app.GetRequiredService<IVehicleService>();
        var vehicle = await vehicleService.CreateAsync(new Vehicle { VehicleNumberRaw = "LWW-1001", PhoneNumber = "0778100104" });
        var older = new DateTime(2026, 8, 6, 8, 0, 0, DateTimeKind.Utc);
        var newer = older.AddHours(1);

        await using (var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]))
        {
            var entity = await dbContext.Vehicles.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == vehicle.Id);
            entity.UpdatedAt = older;
            entity.IsDirty = false;
            await dbContext.SaveChangesAsync();
        }

        var fake = new FakeCloudSyncClient();
        fake.VehicleRows.Add(VehicleRow(vehicle.SyncId, "LWW-1001 cloud", newer));
        await CreateEngine(app, fake).SyncAsync();

        await using (var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]))
        {
            var entity = await dbContext.Vehicles.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == vehicle.Id);
            Assert.Equal("LWW-1001 cloud", entity.VehicleNumberRaw);
            Assert.False(entity.IsDirty);
            entity.VehicleNumberRaw = "LWW-1001 local";
            entity.UpdatedAt = newer.AddHours(1);
            entity.IsDirty = false;
            await dbContext.SaveChangesAsync();
        }

        fake.VehicleRows.Clear();
        fake.VehicleRows.Add(VehicleRow(vehicle.SyncId, "LWW-1001 stale", newer.AddMinutes(30)));
        await CreateEngine(app, fake).SyncAsync();

        await using var verificationContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        var local = await verificationContext.Vehicles.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == vehicle.Id);
        Assert.Equal("LWW-1001 local", local.VehicleNumberRaw);
        Assert.True(local.IsDirty);
    }

    [Fact]
    public async Task Pull_PersistsMaximumServerWatermark()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var firstUpdatedAt = new DateTime(2026, 8, 6, 11, 0, 0, DateTimeKind.Utc);
        var maximumUpdatedAt = firstUpdatedAt.AddMinutes(5);
        var fake = new FakeCloudSyncClient();
        fake.VehicleRows.Add(VehicleRow(Guid.NewGuid(), "WTR-1001", firstUpdatedAt));
        fake.PromotionRows.Add(PromotionRow(Guid.NewGuid(), "Watermark", maximumUpdatedAt));

        await CreateEngine(app, fake).SyncAsync();

        await using var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        var settings = await dbContext.Settings.SingleAsync();
        Assert.Equal(maximumUpdatedAt, settings.LastSyncedAtUtc);
    }

    [Fact]
    public async Task UnconfiguredOptions_DoNotTouchTheClient()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var fake = new FakeCloudSyncClient();
        var engine = new SyncEngine(fake, app.GetRequiredService<PlateGuardDbContextFactory>(), new CloudSyncOptions(), new TestSyncLog());

        var result = await engine.SyncAsync();

        Assert.Equal(SyncStatus.Unconfigured, result.Status);
        Assert.Equal(0, fake.TotalCalls);
    }

    [Fact]
    public async Task Push_ConflictDoesNotAbortOtherRows()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var vehicleService = app.GetRequiredService<IVehicleService>();
        var rejected = await vehicleService.CreateAsync(new Vehicle { VehicleNumberRaw = "CNF-1001", PhoneNumber = "0778100105" });
        var accepted = await vehicleService.CreateAsync(new Vehicle { VehicleNumberRaw = "CNF-1002", PhoneNumber = "0778100106" });
        var fake = new FakeCloudSyncClient();
        fake.RejectedVehicleSyncIds.Add(rejected.SyncId);

        var result = await CreateEngine(app, fake).SyncAsync();

        Assert.Equal(1, result.Conflicts);
        Assert.Equal(3, fake.VehicleUpsertCalls);
        await using var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        Assert.True((await dbContext.Vehicles.IgnoreQueryFilters().SingleAsync(entity => entity.Id == rejected.Id)).IsDirty);
        Assert.False((await dbContext.Vehicles.IgnoreQueryFilters().SingleAsync(entity => entity.Id == accepted.Id)).IsDirty);
    }

    private static SyncEngine CreateEngine(IntegrationTestApp app, FakeCloudSyncClient fake)
    {
        return new SyncEngine(fake, app.GetRequiredService<PlateGuardDbContextFactory>(), new CloudSyncOptions
        {
            SupabaseUrl = "configured",
            SupabaseAnonKey = "configured",
            ServiceAccountEmail = "configured",
            ServiceAccountPassword = "configured"
        }, new TestSyncLog());
    }

    private static async Task<Promotion> CreatePromotionAsync(IntegrationTestApp app, string name)
    {
        return await app.GetRequiredService<IPromotionService>().CreateAsync(new Promotion { PromotionName = name, IsActive = true });
    }

    private static Task<SavePromotionUsageResult> SaveUsageAsync(IntegrationTestApp app, int promotionId, string vehicleNumber, string phoneNumber)
    {
        return app.GetRequiredService<IPromotionUsageService>().SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = vehicleNumber,
            PhoneNumber = phoneNumber,
            PromotionId = promotionId,
            ServiceDate = new DateTime(2026, 8, 6)
        });
    }

    private static VehicleRow VehicleRow(Guid syncId, string number, DateTime updatedAtUtc) => new()
    {
        SyncId = syncId,
        VehicleNumberRaw = number,
        VehicleNumberNormalized = number.Replace("-", string.Empty),
        PhoneNumber = "0778999999",
        CreatedAt = updatedAtUtc.AddMinutes(-1),
        UpdatedAtUtc = updatedAtUtc
    };

    private static PromotionRow PromotionRow(Guid syncId, string name, DateTime updatedAtUtc) => new()
    {
        SyncId = syncId,
        PromotionName = name,
        IsActive = true,
        CreatedAt = updatedAtUtc.AddMinutes(-1),
        UpdatedAtUtc = updatedAtUtc
    };

    private sealed class TestSyncLog : ISyncLog
    {
        public void Info(string message)
        {
        }
    }
}
