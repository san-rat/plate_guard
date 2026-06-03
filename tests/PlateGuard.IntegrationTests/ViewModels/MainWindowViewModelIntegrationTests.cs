using PlateGuard.App.ViewModels;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.IntegrationTests.Infrastructure;

namespace PlateGuard.IntegrationTests.ViewModels;

public sealed class MainWindowViewModelIntegrationTests
{
    [Fact]
    public async Task SearchForUnknownVehicle_EnablesAddUsageForActivePromotion()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "UI Promo",
            IsActive = true
        });

        var viewModel = new MainWindowViewModel(
            app.GetRequiredService<IVehicleService>(),
            app.GetRequiredService<IPromotionService>(),
            app.GetRequiredService<IPromotionUsageService>(),
            app.GetRequiredService<ISettingsService>(),
            app.GetRequiredService<IExportService>());

        await TestWait.UntilAsync(
            () => viewModel.ActivePromotions.Count > 0 && viewModel.SelectedPromotion is not null,
            "Main window view model did not load active promotions.");

        viewModel.SearchText = "CSA-4653";

        await TestWait.UntilAsync(
            () => viewModel.EmptyStateTitle == "No matching vehicle found" && viewModel.SearchModeLabel == "Searching by vehicle number",
            "Main window view model did not enter no-match vehicle search state.");

        Assert.True(viewModel.CanAddUsage);
        Assert.Equal("New vehicle can be added for this promotion", viewModel.EligibilityTitle);
        Assert.Equal("No matching vehicle found", viewModel.EmptyStateTitle);
    }

    [Fact]
    public async Task SearchByPhone_WithNoResults_EnablesAddUsage()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "Phone Search Promo",
            IsActive = true
        });

        var viewModel = new MainWindowViewModel(
            app.GetRequiredService<IVehicleService>(),
            app.GetRequiredService<IPromotionService>(),
            app.GetRequiredService<IPromotionUsageService>(),
            app.GetRequiredService<ISettingsService>(),
            app.GetRequiredService<IExportService>());

        await TestWait.UntilAsync(
            () => viewModel.ActivePromotions.Count > 0 && viewModel.SelectedPromotion is not null,
            "Main window view model did not load active promotions.");

        viewModel.SearchText = "0771234567";

        await TestWait.UntilAsync(
            () => viewModel.HasNoSearchResults && viewModel.SearchModeLabel == "Searching by phone number",
            "Main window view model did not complete phone number search.");

        Assert.True(viewModel.CanAddUsage);
        Assert.Equal("New vehicle can be added for this promotion", viewModel.EligibilityTitle);
    }

    [Fact]
    public async Task SelectedVehicle_WithActivePromotion_EnablesRegisterNewVehicle()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();

        var promotion = await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "New Vehicle Test Promo",
            IsActive = true
        });

        await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CAB-9999",
            PhoneNumber = "0779999999",
            OwnerName = "Test Owner",
            PromotionId = promotion.Id
        });

        var viewModel = new MainWindowViewModel(
            app.GetRequiredService<IVehicleService>(),
            app.GetRequiredService<IPromotionService>(),
            app.GetRequiredService<IPromotionUsageService>(),
            app.GetRequiredService<ISettingsService>(),
            app.GetRequiredService<IExportService>());

        await TestWait.UntilAsync(
            () => viewModel.ActivePromotions.Count > 0 && viewModel.SelectedPromotion is not null,
            "Main window view model did not load active promotions.");

        viewModel.SearchText = "CAB-9999";

        await TestWait.UntilAsync(
            () => viewModel.HasSearchResults && viewModel.SelectedVehicle is not null,
            "Main window view model did not find the vehicle.");

        await TestWait.UntilAsync(
            () => viewModel.CanRegisterNewVehicle,
            "Main window view model did not set CanRegisterNewVehicle after eligibility refresh.");

        Assert.True(viewModel.CanRegisterNewVehicle);
    }
}
