using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Data.Db;
using PlateGuard.IntegrationTests.Infrastructure;

namespace PlateGuard.IntegrationTests.ServiceFlows;

public sealed class CloudSyncFoundationIntegrationTests
{
    [Fact]
    public async Task SyncIds_AreAssignedAndDistinctWhenRowsAreSaved()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();

        var promotion = await promotionService.CreateAsync(new Promotion { PromotionName = "Sync Id Promo", IsActive = true });
        var firstSave = await SaveUsageAsync(promotionUsageService, promotion.Id, "SYN-1001", "0771001001");
        var secondSave = await SaveUsageAsync(promotionUsageService, promotion.Id, "SYN-1002", "0771001002");

        Assert.True(firstSave.IsSuccess);
        Assert.True(secondSave.IsSuccess);

        await using var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        var syncIds = (await dbContext.Vehicles.IgnoreQueryFilters().Select(vehicle => vehicle.SyncId).ToListAsync())
            .Concat(await dbContext.Promotions.IgnoreQueryFilters().Select(promotion => promotion.SyncId).ToListAsync())
            .Concat(await dbContext.PromotionUsages.IgnoreQueryFilters().Select(usage => usage.SyncId).ToListAsync())
            .ToList();

        Assert.All(syncIds, syncId => Assert.NotEqual(Guid.Empty, syncId));
        Assert.Equal(syncIds.Count, syncIds.Distinct().Count());
    }

    [Fact]
    public async Task SoftDelete_HidesTheUsageFromAllRepositoryReadPaths()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();
        var promotionUsageRepository = app.GetRequiredService<IPromotionUsageRepository>();
        var promotion = await promotionService.CreateAsync(new Promotion { PromotionName = "Soft Delete Reads", IsActive = true });
        var save = await SaveUsageAsync(promotionUsageService, promotion.Id, "SDL-1001", "0772001001");

        var deleteResult = await promotionUsageService.DeleteUsageAsync(save.PromotionUsage!.Id, "admin");

        Assert.True(deleteResult.IsSuccess);
        Assert.Empty(await promotionUsageService.SearchUsageRecordsAsync(new PromotionUsageRecordQuery()));
        Assert.Equal(0, await promotionUsageService.GetUsageCountForPromotionAsync(promotion.Id));
        Assert.False(await promotionUsageRepository.ExistsAsync(save.Vehicle!.Id, promotion.Id));
    }

    [Fact]
    public async Task SoftDelete_LeavesTheUsagePhysicallyPresent()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();
        var promotion = await promotionService.CreateAsync(new Promotion { PromotionName = "Soft Delete Row", IsActive = true });
        var save = await SaveUsageAsync(promotionUsageService, promotion.Id, "SDP-1001", "0773001001");

        Assert.True((await promotionUsageService.DeleteUsageAsync(save.PromotionUsage!.Id, "admin")).IsSuccess);

        await using var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        var entity = await dbContext.PromotionUsages.IgnoreQueryFilters().SingleAsync(usage => usage.Id == save.PromotionUsage.Id);

        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedAtUtc);
    }

    [Fact]
    public async Task SoftDeletedUsage_AllowsTheSameVehicleToRedeemAgain()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();
        var promotion = await promotionService.CreateAsync(new Promotion { PromotionName = "Re-redeem", IsActive = true });
        var firstSave = await SaveUsageAsync(promotionUsageService, promotion.Id, "RDM-1001", "0774001001");

        Assert.True((await promotionUsageService.DeleteUsageAsync(firstSave.PromotionUsage!.Id, "admin")).IsSuccess);

        var secondSave = await SaveUsageAsync(promotionUsageService, promotion.Id, "RDM-1001", "0774001001");

        Assert.True(secondSave.IsSuccess);
    }

    [Fact]
    public async Task LiveUsage_StillRejectsASecondRedemption()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();
        var promotion = await promotionService.CreateAsync(new Promotion { PromotionName = "Live Rule", IsActive = true });

        Assert.True((await SaveUsageAsync(promotionUsageService, promotion.Id, "LIV-1001", "0775001001")).IsSuccess);

        var duplicateSave = await SaveUsageAsync(promotionUsageService, promotion.Id, "LIV-1001", "0775001001");

        Assert.False(duplicateSave.IsSuccess);
        Assert.Equal("Promotion already used for this vehicle.", duplicateSave.Message);
    }

    [Fact]
    public async Task UpdateUsage_SetsIsDirty()
    {
        await using var app = await IntegrationTestApp.CreateAsync();
        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();
        var promotion = await promotionService.CreateAsync(new Promotion { PromotionName = "Dirty Update", IsActive = true });
        var save = await SaveUsageAsync(promotionUsageService, promotion.Id, "DRT-1001", "0776001001");

        await using (var dbContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]))
        {
            var entity = await dbContext.PromotionUsages.IgnoreQueryFilters().SingleAsync(usage => usage.Id == save.PromotionUsage!.Id);
            entity.IsDirty = false;
            await dbContext.SaveChangesAsync();
        }

        var usage = save.PromotionUsage!;
        usage.Notes = "Updated for dirty tracking";
        var updateResult = await promotionUsageService.UpdateUsageAsync(usage);

        Assert.True(updateResult.IsSuccess);

        await using var verificationContext = app.GetRequiredService<PlateGuardDbContextFactory>().CreateDbContext([]);
        var updatedEntity = await verificationContext.PromotionUsages.IgnoreQueryFilters().SingleAsync(entity => entity.Id == usage.Id);
        Assert.True(updatedEntity.IsDirty);
    }

    private static Task<SavePromotionUsageResult> SaveUsageAsync(
        IPromotionUsageService promotionUsageService,
        int promotionId,
        string vehicleNumber,
        string phoneNumber)
    {
        return promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = vehicleNumber,
            PhoneNumber = phoneNumber,
            PromotionId = promotionId,
            ServiceDate = new DateTime(2026, 8, 6)
        });
    }
}
