using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.IntegrationTests.Infrastructure;

namespace PlateGuard.IntegrationTests.ServiceFlows;

public sealed class PromotionUsageIntegrationTests
{
    [Fact]
    public async Task DatabaseInitialization_SeedsDefaultSettingsRow()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var settingsRepository = app.GetRequiredService<ISettingsRepository>();
        var settings = await settingsRepository.GetAsync();

        Assert.NotNull(settings);
        Assert.Equal(AppSettings.DefaultId, settings!.Id);
        Assert.NotNull(settings.DeletePasswordHash);
        Assert.Equal(64, settings.DeletePasswordHash!.Length);
    }

    [Fact]
    public async Task SaveVehicleAndUsage_PersistsAcrossRestart()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();
        var vehicleService = app.GetRequiredService<IVehicleService>();

        var promotion = await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "Restart Promo",
            Description = "Persistence check",
            IsActive = true
        });

        var saveResult = await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CAB-1234",
            PhoneNumber = "0771234567",
            OwnerName = "Nimal Perera",
            Brand = "Toyota",
            Model = "Corolla",
            PromotionId = promotion.Id,
            ServiceDate = new DateTime(2026, 4, 9, 16, 30, 0),
            AmountPaid = 5000m
        });

        Assert.True(saveResult.IsSuccess);
        Assert.NotNull(saveResult.Vehicle);
        Assert.NotNull(saveResult.PromotionUsage);

        var beforeRestartVehicle = await vehicleService.FindByVehicleNumberAsync("cab 1234");
        Assert.NotNull(beforeRestartVehicle);
        Assert.Equal("CAB1234", beforeRestartVehicle!.VehicleNumberNormalized);

        await using var restartedApp = await app.CreateRestartedAsync();
        var restartedVehicleService = restartedApp.GetRequiredService<IVehicleService>();
        var restartedPromotionUsageService = restartedApp.GetRequiredService<IPromotionUsageService>();

        var afterRestartVehicle = await restartedVehicleService.FindByVehicleNumberAsync("CAB-1234");
        Assert.NotNull(afterRestartVehicle);

        var history = await restartedPromotionUsageService.GetUsageHistoryForVehicleAsync(afterRestartVehicle!.Id);
        Assert.Single(history);
        Assert.Equal(saveResult.PromotionUsage!.Id, history[0].Id);
        Assert.Equal(new DateTime(2026, 4, 9), history[0].ServiceDate);
    }

    [Fact]
    public async Task DuplicateUsageIsBlockedButDifferentPromotionIsAllowed()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();

        var firstPromotion = await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "Promo A",
            IsActive = true
        });
        var secondPromotion = await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "Promo B",
            IsActive = true
        });

        var firstSave = await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CSA-4653",
            PhoneNumber = "0712345678",
            PromotionId = firstPromotion.Id
        });
        Assert.True(firstSave.IsSuccess);

        var duplicateSave = await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CSA 4653",
            PhoneNumber = "0712345678",
            PromotionId = firstPromotion.Id
        });
        Assert.False(duplicateSave.IsSuccess);
        Assert.Equal("Promotion already used for this vehicle.", duplicateSave.Message);

        var secondSave = await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CSA-4653",
            PhoneNumber = "0712345678",
            PromotionId = secondPromotion.Id
        });
        Assert.True(secondSave.IsSuccess);

        var history = await promotionUsageService.GetUsageHistoryForVehicleAsync(firstSave.Vehicle!.Id);
        Assert.Equal(2, history.Count);
    }
}
