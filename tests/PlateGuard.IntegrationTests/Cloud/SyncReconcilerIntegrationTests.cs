using Microsoft.EntityFrameworkCore;
using PlateGuard.Cloud;
using PlateGuard.Cloud.Dtos;
using PlateGuard.Data.Db;
using PlateGuard.Data.Entities;
using PlateGuard.IntegrationTests.Infrastructure;

namespace PlateGuard.IntegrationTests.Cloud;

public sealed class SyncReconcilerIntegrationTests
{
    [Fact]
    public async Task BatchedPush_UsesOneVehicleRequestForTenRows()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        await using (var db = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]))
        {
            for (var index = 0; index < 10; index++) db.Vehicles.Add(new VehicleEntity { VehicleNumberRaw = $"BAT-{index}", VehicleNumberNormalized = $"BAT{index}", PhoneNumber = "0778000000", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        var fake = new FakeCloudSyncClient();

        await Engine(app, fake).SyncAsync();

        Assert.Equal(1, fake.VehicleUpsertCalls);
    }

    [Fact]
    public async Task DuplicateVehicle_RelabelsLocalSyncIdAndRecordsConflict()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var cloudId = Guid.NewGuid();
        var localId = Guid.NewGuid();
        await using (var db = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]))
        {
            db.Vehicles.Add(new VehicleEntity { SyncId = localId, VehicleNumberRaw = "DUP-1001", VehicleNumberNormalized = "DUP1001", PhoneNumber = "0778000001", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, IsDirty = true });
            await db.SaveChangesAsync();
        }
        var fake = new FakeCloudSyncClient();
        fake.RejectedVehicleSyncIds.Add(localId);
        fake.VehicleRows.Add(new VehicleRow { SyncId = cloudId, VehicleNumberRaw = "DUP-1001", VehicleNumberNormalized = "DUP1001", PhoneNumber = "0778000001", CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-1) });

        var result = await Engine(app, fake).SyncAsync();

        await using var verify = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        Assert.NotNull(await verify.Vehicles.IgnoreQueryFilters().SingleOrDefaultAsync(vehicle => vehicle.SyncId == cloudId));
        Assert.Contains(await verify.SyncConflicts.ToListAsync(), conflict => conflict.Kind == "DuplicateVehicle");
        Assert.Equal(1, result.ConflictsResolved);
    }

    [Fact]
    public async Task UnresolvableVehicle_RejectionStaysDirty()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var syncId = Guid.NewGuid();
        await using (var db = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]))
        {
            db.Vehicles.Add(new VehicleEntity { SyncId = syncId, VehicleNumberRaw = "UNR-1001", VehicleNumberNormalized = "UNR1001", PhoneNumber = "0778000002", CreatedAt = DateTime.UtcNow, IsDirty = true });
            await db.SaveChangesAsync();
        }
        var fake = new FakeCloudSyncClient();
        fake.RejectedVehicleSyncIds.Add(syncId);

        var result = await Engine(app, fake).SyncAsync();

        await using var verify = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        Assert.True((await verify.Vehicles.IgnoreQueryFilters().SingleAsync(vehicle => vehicle.SyncId == syncId)).IsDirty);
        Assert.Equal(1, result.ConflictsUnresolved);
    }

    [Fact]
    public async Task DuplicateVehicle_MergesIntoSurvivorAndDropsCollidingRedemption()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var cloudId = Guid.NewGuid();
        var losingId = Guid.NewGuid();
        int losingVehicleId;
        int survivorVehicleId;

        await using (var db = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]))
        {
            var promotion = new PromotionEntity { PromotionName = "Merge Promo", CreatedAt = DateTime.UtcNow };
            db.Promotions.Add(promotion);

            // The survivor already carries the cloud SyncId under a different plate.
            var survivor = new VehicleEntity { SyncId = cloudId, VehicleNumberRaw = "SUR-2001", VehicleNumberNormalized = "SUR2001", PhoneNumber = "0779000001", CreatedAt = DateTime.UtcNow };
            var losing = new VehicleEntity { SyncId = losingId, VehicleNumberRaw = "MRG-1001", VehicleNumberNormalized = "MRG1001", PhoneNumber = "0779000002", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, IsDirty = true };
            db.Vehicles.AddRange(survivor, losing);
            await db.SaveChangesAsync();

            survivorVehicleId = survivor.Id;
            losingVehicleId = losing.Id;

            // Both vehicles hold a live usage of the same promotion, so the merge must drop one.
            db.PromotionUsages.Add(new PromotionUsageEntity { VehicleId = survivor.Id, PromotionId = promotion.Id, ServiceDate = new DateTime(2026, 8, 1), CreatedAt = DateTime.UtcNow });
            db.PromotionUsages.Add(new PromotionUsageEntity { VehicleId = losing.Id, PromotionId = promotion.Id, ServiceDate = new DateTime(2026, 8, 2), CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var fake = new FakeCloudSyncClient();
        fake.RejectedVehicleSyncIds.Add(losingId);
        fake.VehicleRows.Add(new VehicleRow { SyncId = cloudId, VehicleNumberRaw = "MRG-1001", VehicleNumberNormalized = "MRG1001", PhoneNumber = "0779000002", CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-1) });

        var result = await Engine(app, fake).SyncAsync();

        Assert.Equal(SyncStatus.Success, result.Status);

        await using var verify = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        Assert.Null(await verify.Vehicles.IgnoreQueryFilters().SingleOrDefaultAsync(vehicle => vehicle.Id == losingVehicleId));

        var usages = await verify.PromotionUsages.IgnoreQueryFilters().ToListAsync();
        Assert.All(usages, usage => Assert.Equal(survivorVehicleId, usage.VehicleId));
        Assert.Single(usages, usage => !usage.IsDeleted);
        Assert.Single(usages, usage => usage.IsDeleted);
    }

    [Fact]
    public async Task DuplicateRedemption_DropsLocalLoserAndRecordsReviewableConflict()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var vehicleSyncId = Guid.NewGuid();
        var promotionSyncId = Guid.NewGuid();
        var losingUsageSyncId = Guid.NewGuid();
        var serviceDate = new DateTime(2026, 8, 3);

        await using (var db = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]))
        {
            var vehicle = new VehicleEntity { SyncId = vehicleSyncId, VehicleNumberRaw = "DBL-1001", VehicleNumberNormalized = "DBL1001", PhoneNumber = "0779000003", CreatedAt = DateTime.UtcNow, IsDirty = false };
            var promotion = new PromotionEntity { SyncId = promotionSyncId, PromotionName = "Double Promo", CreatedAt = DateTime.UtcNow, IsDirty = false };
            db.Vehicles.Add(vehicle);
            db.Promotions.Add(promotion);
            await db.SaveChangesAsync();

            db.PromotionUsages.Add(new PromotionUsageEntity { SyncId = losingUsageSyncId, VehicleId = vehicle.Id, PromotionId = promotion.Id, ServiceDate = serviceDate, CreatedAt = DateTime.UtcNow, IsDirty = true });
            await db.SaveChangesAsync();
        }

        var fake = new FakeCloudSyncClient();
        fake.RejectedPromotionUsageSyncIds.Add(losingUsageSyncId);
        // The cloud already holds a different redemption for the same vehicle and promotion.
        fake.PromotionUsageRows.Add(new PromotionUsageRow { SyncId = Guid.NewGuid(), VehicleSyncId = vehicleSyncId, PromotionSyncId = promotionSyncId, ServiceDate = new DateTime(2026, 8, 1), CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-1) });

        var result = await Engine(app, fake).SyncAsync();

        Assert.Equal(SyncStatus.Success, result.Status);
        Assert.Equal(1, result.ConflictsResolved);

        await using var verify = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        var loser = await verify.PromotionUsages.IgnoreQueryFilters().SingleAsync(usage => usage.SyncId == losingUsageSyncId);
        Assert.True(loser.IsDeleted);
        Assert.False(loser.IsDirty);

        var conflict = await verify.SyncConflicts.SingleAsync(entry => entry.Kind == "DuplicateRedemption");
        Assert.Equal("DBL-1001", conflict.VehicleNumberRaw);
        Assert.Equal("Double Promo", conflict.PromotionName);
        Assert.Equal(serviceDate, conflict.ServiceDate);
    }

    [Fact]
    public async Task SyncConflicts_AreNeverPushedToTheCloud()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var syncId = Guid.NewGuid();
        await using (var db = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]))
        {
            db.Vehicles.Add(new VehicleEntity { SyncId = syncId, VehicleNumberRaw = "NPS-1001", VehicleNumberNormalized = "NPS1001", PhoneNumber = "0779000004", CreatedAt = DateTime.UtcNow, IsDirty = true });
            await db.SaveChangesAsync();
        }

        var fake = new FakeCloudSyncClient();
        fake.RejectedVehicleSyncIds.Add(syncId);

        await Engine(app, fake).SyncAsync();

        await using var verify = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        Assert.NotEmpty(await verify.SyncConflicts.ToListAsync());
        Assert.Empty(fake.ReceivedPromotions);
        Assert.DoesNotContain(fake.ReceivedVehicles, row => row.VehicleNumberRaw is null);
    }

    private static SyncEngine Engine(IntegrationTestApp app, FakeCloudSyncClient fake)
    {
        return new SyncEngine(fake, app.GetRequiredService<PlateGuardDbContextFactory>(), new CloudSyncOptions { SupabaseUrl = "configured", SupabaseAnonKey = "configured", ServiceAccountEmail = "configured", ServiceAccountPassword = "configured" }, new Log(), new SyncReconciler(fake));
    }

    private sealed class Log : ISyncLog { public void Info(string message) { } }
}
