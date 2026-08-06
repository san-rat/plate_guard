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

    private static SyncEngine Engine(IntegrationTestApp app, FakeCloudSyncClient fake)
    {
        return new SyncEngine(fake, app.GetRequiredService<PlateGuardDbContextFactory>(), new CloudSyncOptions { SupabaseUrl = "configured", SupabaseAnonKey = "configured", ServiceAccountEmail = "configured", ServiceAccountPassword = "configured" }, new Log(), new SyncReconciler(fake));
    }

    private sealed class Log : ISyncLog { public void Info(string message) { } }
}
