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

    [Fact]
    public async Task EligibilityHint_ReportsDuplicateAndFreshVehicleStates()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();
        var vehicleService = app.GetRequiredService<IVehicleService>();

        var promotion = await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "Eligibility Promo",
            IsActive = true
        });

        await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CAB-2001",
            PhoneNumber = "0772002001",
            OwnerName = "Eligibility Owner",
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

        viewModel.VehicleNumberRaw = "CAB-2001";

        await TestWait.UntilAsync(
            () => viewModel.IsEligibilityHintNegative,
            "Eligibility hint did not report duplicate usage.");

        Assert.Equal("Promotion already used for this vehicle.", viewModel.EligibilityHint);

        viewModel.VehicleNumberRaw = "CAB-2002";

        await TestWait.UntilAsync(
            () => viewModel.IsEligibilityHintPositive,
            "Eligibility hint did not report a fresh vehicle as eligible.");

        Assert.Equal("Eligible — this vehicle can use this promotion.", viewModel.EligibilityHint);
    }

    [Fact]
    public async Task ServiceDate_DefaultsToTodayAndChosenDateIsSaved()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();
        var vehicleService = app.GetRequiredService<IVehicleService>();

        var promotion = await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "Date Promo",
            IsActive = true
        });

        var viewModel = new AddUsageDialogViewModel(
            promotionUsageService,
            vehicleService,
            new AddUsageDialogRequest
            {
                SelectedPromotion = promotion,
                AvailablePromotions = [promotion]
            });

        Assert.Equal(DateTimeOffset.Now.Date, viewModel.ServiceDate!.Value.Date);

        viewModel.VehicleNumberRaw = "CAB-3001";
        viewModel.PhoneNumber = "0773003001";
        viewModel.ServiceDate = new DateTimeOffset(2026, 5, 12, 15, 30, 0, TimeSpan.Zero);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.LastSaveResult?.IsSuccess);

        var records = await promotionUsageService.SearchUsageRecordsAsync(new PromotionUsageRecordQuery());
        var savedRecord = Assert.Single(records);
        Assert.Equal(new DateTime(2026, 5, 12), savedRecord.ServiceDate);
    }
}
