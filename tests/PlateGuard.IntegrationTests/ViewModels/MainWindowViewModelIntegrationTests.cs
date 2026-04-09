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
}
