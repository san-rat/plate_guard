using PlateGuard.App.ViewModels;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.IntegrationTests.Infrastructure;

namespace PlateGuard.IntegrationTests.ViewModels;

public sealed class AddUsageDialogViewModelIntegrationTests
{
    [Fact]
    public async Task OwnerSuggestions_CanApplyOwnerAndReuseVehicleDetails()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();
        var vehicleService = app.GetRequiredService<IVehicleService>();

        var promotion = await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "Owner Suggestion Promo",
            IsActive = true
        });

        await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CAB-1001",
            PhoneNumber = "0771001001",
            OwnerName = "Kamala Silva",
            Brand = "Toyota",
            Model = "Aqua",
            PromotionId = promotion.Id
        });

        await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CAD-1002",
            PhoneNumber = "0771001001",
            OwnerName = "Kamala Silva",
            Brand = "Honda",
            Model = "Fit",
            PromotionId = promotion.Id
        });

        var viewModel = new AddUsageDialogViewModel(
            promotionUsageService,
            vehicleService,
            new AddUsageDialogRequest
            {
                SelectedPromotion = promotion,
                AvailablePromotions = [promotion]
            });

        viewModel.OwnerName = "Ka";

        await TestWait.UntilAsync(
            () => viewModel.OwnerSuggestions.Count == 1 && viewModel.IsOwnerSuggestionsOpen,
            "Owner suggestions did not populate after debounce.");

        var suggestion = viewModel.OwnerSuggestions.Single();
        Assert.Equal("Kamala Silva", suggestion.OwnerName);
        Assert.Equal("0771001001", suggestion.PhoneNumber);
        Assert.Equal(2, suggestion.Vehicles.Count);

        viewModel.ApplyOwnerSuggestionCommand.Execute(suggestion);

        Assert.Equal("Kamala Silva", viewModel.OwnerName);
        Assert.Equal("0771001001", viewModel.PhoneNumber);
        Assert.False(viewModel.IsOwnerSuggestionsOpen);
        Assert.True(viewModel.HasReusableVehicles);
        Assert.Equal(2, viewModel.ReusableVehicles.Count);

        var reusableVehicle = viewModel.ReusableVehicles.Single(vehicle => vehicle.VehicleNumberRaw == "CAD-1002");
        viewModel.ReuseVehicleCommand.Execute(reusableVehicle);

        Assert.Equal("CAD-1002", viewModel.VehicleNumberRaw);
        Assert.Equal("Honda", viewModel.Brand);
        Assert.Equal("Fit", viewModel.Model);
        Assert.Equal("CAD1002", viewModel.NormalizedVehicleNumber);
    }
}
