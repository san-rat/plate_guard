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
    public async Task SearchModeSelector_ForcesShortPhoneSearchWhileAutoPreservesHeuristic()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();

        var promotion = await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "Forced Phone Promo",
            IsActive = true
        });

        await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "PHN-4321",
            PhoneNumber = "0771234567",
            OwnerName = "Phone Search Owner",
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

        viewModel.SelectedSearchModeOption = viewModel.SearchModeOptions.Single(option => option.Mode == SearchMode.PhoneNumber);
        viewModel.SearchText = "1234";

        await TestWait.UntilAsync(
            () => viewModel.SearchModeLabel == "Searching by phone number" && viewModel.HasSearchResults,
            "Forced phone search did not find a short phone fragment.");

        Assert.Equal("PHN-4321", viewModel.SelectedVehicle?.VehicleNumberRaw);

        viewModel.SelectedSearchModeOption = viewModel.SearchModeOptions.Single(option => option.Mode == SearchMode.Auto);

        await TestWait.UntilAsync(
            () => viewModel.SearchModeLabel == "Searching by owner name" && viewModel.HasNoSearchResults,
            "Auto mode did not preserve the existing short numeric owner-name heuristic.");

        Assert.True(viewModel.CanAddUsage);
    }

    [Fact]
    public async Task SettingsCommands_RaiseNotificationEventsForSuccessAndError()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var viewModel = new MainWindowViewModel(
            app.GetRequiredService<IVehicleService>(),
            app.GetRequiredService<IPromotionService>(),
            app.GetRequiredService<IPromotionUsageService>(),
            app.GetRequiredService<ISettingsService>(),
            app.GetRequiredService<IExportService>());

        await TestWait.UntilAsync(
            () => viewModel.SettingsStatusMessage == "Settings loaded.",
            "Main window view model did not load settings.");

        var notifications = new List<(string Message, bool IsError)>();
        viewModel.NotificationRequested += notification => notifications.Add(notification);

        viewModel.SettingsShopName = "Notify Shop";
        viewModel.SettingsExportFolder = Path.GetFullPath(Path.Combine(app.RootDirectory, "exports"));

        await viewModel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.Contains(notifications, notification =>
            notification.Message == "Settings saved successfully." && !notification.IsError);

        viewModel.SettingsExportFolder = "relative\\exports";

        await viewModel.SaveSettingsCommand.ExecuteAsync(null);

        Assert.Contains(notifications, notification =>
            notification.Message == "Export folder must be a full folder path." && notification.IsError);
    }

    [Fact]
    public async Task DeletePasswordWarning_IsLoadedAndClearsAfterPasswordChange()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var viewModel = new MainWindowViewModel(
            app.GetRequiredService<IVehicleService>(),
            app.GetRequiredService<IPromotionService>(),
            app.GetRequiredService<IPromotionUsageService>(),
            app.GetRequiredService<ISettingsService>(),
            app.GetRequiredService<IExportService>());

        await TestWait.UntilAsync(
            () => viewModel.SettingsStatusMessage == "Settings loaded.",
            "Main window view model did not load settings.");

        Assert.True(viewModel.IsDeletePasswordDefault);
        Assert.Equal("Default delete password is still active. Change it before regular use.", viewModel.DeletePasswordDefaultWarning);

        viewModel.CurrentDeletePassword = "admin";
        viewModel.NewDeletePassword = "new-secret";
        viewModel.ConfirmDeletePassword = "new-secret";

        await viewModel.ChangeDeletePasswordCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsDeletePasswordDefault);
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

    [Fact]
    public async Task SearchWithResults_DoesNotEnableAddUsageUntilSelectionIsConfirmed()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var vehicleService = app.GetRequiredService<IVehicleService>();
        await promotionService.CreateAsync(new Promotion { PromotionName = "Selection Promo", IsActive = true });
        await vehicleService.CreateAsync(new Vehicle { VehicleNumberRaw = "SEL-1001", PhoneNumber = "0771001001" });

        var viewModel = new MainWindowViewModel(
            vehicleService,
            promotionService,
            app.GetRequiredService<IPromotionUsageService>(),
            app.GetRequiredService<ISettingsService>(),
            app.GetRequiredService<IExportService>());

        await TestWait.UntilAsync(
            () => viewModel.ActivePromotions.Count > 0 && viewModel.SelectedPromotion is not null,
            "Main window view model did not load active promotions.");

        await TestWait.UntilAsync(
            () => viewModel.SettingsStatusMessage == "Settings loaded.",
            "Main window view model did not finish initialization.");

        viewModel.SearchText = "SEL-1001";

        await TestWait.UntilAsync(
            () => viewModel.HasSearchResults && viewModel.SelectedVehicle is not null,
            "Main window view model did not find the vehicle.");

        Assert.NotNull(viewModel.SelectedVehicle);
        Assert.False(viewModel.CanAddUsage);
    }

    [Fact]
    public async Task SearchWithResults_EnablesAddUsageAfterExplicitSelection()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var vehicleService = app.GetRequiredService<IVehicleService>();
        await promotionService.CreateAsync(new Promotion { PromotionName = "Confirmation Promo", IsActive = true });
        await vehicleService.CreateAsync(new Vehicle { VehicleNumberRaw = "SEL-1002", PhoneNumber = "0771001002" });

        var viewModel = new MainWindowViewModel(
            vehicleService,
            promotionService,
            app.GetRequiredService<IPromotionUsageService>(),
            app.GetRequiredService<ISettingsService>(),
            app.GetRequiredService<IExportService>());

        await TestWait.UntilAsync(
            () => viewModel.ActivePromotions.Count > 0 && viewModel.SelectedPromotion is not null,
            "Main window view model did not load active promotions.");

        await TestWait.UntilAsync(
            () => viewModel.SettingsStatusMessage == "Settings loaded.",
            "Main window view model did not finish initialization.");

        viewModel.SearchText = "SEL-1002";

        await TestWait.UntilAsync(
            () => viewModel.HasSearchResults && viewModel.SelectedVehicle is not null,
            "Main window view model did not find the vehicle.");

        viewModel.SelectedVehicle = viewModel.SearchResults[0];

        await TestWait.UntilAsync(
            () => viewModel.CanAddUsage,
            "Main window view model did not enable Add Usage after explicit selection.");

        Assert.True(viewModel.CanAddUsage);
    }

    [Fact]
    public async Task ChangingSearchText_ResetsSelectionConfirmation()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var vehicleService = app.GetRequiredService<IVehicleService>();
        await promotionService.CreateAsync(new Promotion { PromotionName = "Reset Promo", IsActive = true });
        await vehicleService.CreateAsync(new Vehicle { VehicleNumberRaw = "RST-1001", PhoneNumber = "0771001003" });
        await vehicleService.CreateAsync(new Vehicle { VehicleNumberRaw = "RST-1002", PhoneNumber = "0771001004" });

        var viewModel = new MainWindowViewModel(
            vehicleService,
            promotionService,
            app.GetRequiredService<IPromotionUsageService>(),
            app.GetRequiredService<ISettingsService>(),
            app.GetRequiredService<IExportService>());

        await TestWait.UntilAsync(
            () => viewModel.ActivePromotions.Count > 0 && viewModel.SelectedPromotion is not null,
            "Main window view model did not load active promotions.");

        await TestWait.UntilAsync(
            () => viewModel.SettingsStatusMessage == "Settings loaded.",
            "Main window view model did not finish initialization.");

        viewModel.SearchText = "RST-1001";

        await TestWait.UntilAsync(
            () => viewModel.HasSearchResults && viewModel.SelectedVehicle is not null,
            "Main window view model did not find the first vehicle.");

        viewModel.SelectedVehicle = viewModel.SearchResults[0];

        await TestWait.UntilAsync(
            () => viewModel.CanAddUsage,
            "Main window view model did not enable Add Usage after explicit selection.");

        viewModel.SearchText = "RST-1002";

        await TestWait.UntilAsync(
            () => viewModel.SelectedVehicle?.VehicleNumberRaw == "RST-1002" && !viewModel.CanAddUsage,
            "Main window view model did not reset selection confirmation for the next search.");

        Assert.False(viewModel.CanAddUsage);
    }

    [Fact]
    public async Task EmptySearchWithActivePromotion_AllowsVehicleCreation()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        await promotionService.CreateAsync(new Promotion { PromotionName = "Empty Search Promo", IsActive = true });

        var viewModel = new MainWindowViewModel(
            app.GetRequiredService<IVehicleService>(),
            promotionService,
            app.GetRequiredService<IPromotionUsageService>(),
            app.GetRequiredService<ISettingsService>(),
            app.GetRequiredService<IExportService>());

        await TestWait.UntilAsync(
            () => viewModel.ActivePromotions.Count > 0 && viewModel.SelectedPromotion is not null,
            "Main window view model did not load active promotions.");

        await TestWait.UntilAsync(
            () => viewModel.SettingsStatusMessage == "Settings loaded.",
            "Main window view model did not finish initialization.");

        Assert.True(viewModel.CanCreateVehicleFromEmptyState);

        viewModel.SearchText = "new vehicle";

        Assert.False(viewModel.CanCreateVehicleFromEmptyState);
    }
}
