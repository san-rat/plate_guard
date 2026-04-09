using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Core.Services;
using PlateGuard.App.ViewModels;
using PlateGuard.Data.Db;
using PlateGuard.Data.Repositories;

var smokeDbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "builds", "smoke-tests", "plateguard-smoke.db"));
var smokeDbDirectory = Path.GetDirectoryName(smokeDbPath) ?? throw new InvalidOperationException("Smoke test database directory could not be resolved.");

Directory.CreateDirectory(smokeDbDirectory);
DeleteIfExists(smokeDbPath);
DeleteIfExists($"{smokeDbPath}-shm");
DeleteIfExists($"{smokeDbPath}-wal");

Environment.SetEnvironmentVariable(PlateGuardDatabasePathProvider.DatabasePathEnvironmentVariableName, smokeDbPath);

var initializer = new PlateGuardDatabaseInitializer();
await initializer.InitializeAsync();

var services = CreateServices();

var settings = await services.SettingsRepository.GetAsync() ?? throw new InvalidOperationException("Expected seeded settings row.");
Assert(settings.Id == PlateGuardDatabaseInitializer.DefaultSettingsId, "Default settings row was not seeded.");
Assert(settings.DeletePasswordHash is { Length: 64 }, "Seeded delete password hash is invalid.");

var exportFolder = Path.Combine(smokeDbDirectory, "exports");
var settingsUpdateResult = await services.SettingsService.UpdateAsync(new UpdateAppSettingsRequest
{
    ShopName = "PlateGuard Smoke Shop",
    ExportFolder = exportFolder
});
Assert(settingsUpdateResult.IsSuccess, "Updating settings failed.");
Assert(settingsUpdateResult.Settings?.ShopName == "PlateGuard Smoke Shop", "Shop name did not persist correctly.");
Assert(settingsUpdateResult.Settings?.ExportFolder == exportFolder, "Export folder did not persist correctly.");

var promotion = await services.PromotionService.CreateAsync(new Promotion
{
    PromotionName = "New Year Promo",
    Description = "Smoke test promotion",
    IsActive = true
});

var mainWindowViewModel = new MainWindowViewModel(
    services.VehicleService,
    services.PromotionService,
    services.PromotionUsageService,
    services.SettingsService,
    services.ExportService);
await WaitUntilAsync(
    () => mainWindowViewModel.ActivePromotions.Count > 0 && mainWindowViewModel.SelectedPromotion is not null,
    "Main window view model did not load active promotions.");
mainWindowViewModel.SearchText = "CSA-4653";
await WaitUntilAsync(
    () => mainWindowViewModel.EmptyStateTitle == "No matching vehicle found" && mainWindowViewModel.SearchModeLabel == "Searching by vehicle number",
    "Main window view model did not enter the no-match vehicle search state.");
Assert(mainWindowViewModel.CanAddUsage, "UI state should allow Add Usage for a new vehicle when vehicle-number search has no match.");
Assert(mainWindowViewModel.EligibilityTitle == "New vehicle can be added for this promotion", "Eligibility title for new vehicle add flow is incorrect.");

var blankVehicleResult = await services.PromotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
{
    PromotionId = promotion.Id,
    PhoneNumber = "0771234567"
});
Assert(!blankVehicleResult.IsSuccess && blankVehicleResult.Message == "Vehicle number is required.", "Blank vehicle number validation failed.");

var blankPhoneResult = await services.PromotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
{
    VehicleNumberRaw = "CAB-1234",
    PromotionId = promotion.Id
});
Assert(!blankPhoneResult.IsSuccess && blankPhoneResult.Message == "Phone number is required.", "Blank phone validation failed.");

var noPromotionResult = await services.PromotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
{
    VehicleNumberRaw = "CAB-1234",
    PhoneNumber = "0771234567"
});
Assert(!noPromotionResult.IsSuccess && noPromotionResult.Message == "Promotion is required.", "Missing promotion validation failed.");

var unknownVehicle = await services.VehicleService.FindByVehicleNumberAsync("ZZZ-9999");
Assert(unknownVehicle is null, "Unknown vehicle search should return no result before saving.");

var eligibilityBeforeSave = await services.PromotionUsageService.CheckEligibilityAsync("cab-1234", promotion.Id);
Assert(eligibilityBeforeSave.IsEligible, "Vehicle should be eligible before any usage is saved.");

var saveResult = await services.PromotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
{
    VehicleNumberRaw = "cab-1234",
    PhoneNumber = "0771234567",
    OwnerName = "Nimal Perera",
    Brand = "Toyota",
    Model = "Corolla",
    PromotionId = promotion.Id,
    Mileage = 120000,
    AmountPaid = 5000m,
    ServiceDate = DateTime.UtcNow
});

Assert(saveResult.IsSuccess, "Combined save flow failed.");
Assert(saveResult.Vehicle is not null, "Combined save flow did not return the vehicle.");
Assert(saveResult.PromotionUsage is not null, "Combined save flow did not return the usage record.");

var vehicle = saveResult.Vehicle!;
var usage = saveResult.PromotionUsage!;
Assert(vehicle.VehicleNumberNormalized == "CAB1234", "Vehicle number normalization did not persist correctly.");
Assert(await services.PromotionUsageRepository.ExistsAsync(vehicle.Id, promotion.Id), "Promotion usage existence check failed.");

var eligibilityAfterSave = await services.PromotionUsageService.CheckEligibilityAsync(vehicle.Id, promotion.Id);
Assert(!eligibilityAfterSave.IsEligible, "Vehicle should not be eligible after usage is saved.");
Assert(eligibilityAfterSave.ExistingUsage?.Id == usage.Id, "Eligibility check did not return the existing usage.");

var duplicateResult = await services.PromotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
{
    VehicleNumberRaw = "CAB 1234",
    PhoneNumber = "0771234567",
    PromotionId = promotion.Id,
    ServiceDate = DateTime.UtcNow
});
Assert(!duplicateResult.IsSuccess, "Duplicate promotion usage was not blocked.");

var byPlate = await services.VehicleService.FindByVehicleNumberAsync("CAB 1234");
Assert(byPlate?.Id == vehicle.Id, "Search by normalized plate failed.");

var byPhone = await services.VehicleService.SearchByPhoneNumberAsync("0771");
Assert(byPhone.Count == 1 && byPhone[0].Id == vehicle.Id, "Search by phone number failed.");

var byOwner = await services.VehicleService.SearchByOwnerNameAsync("Nimal");
Assert(byOwner.Count == 1 && byOwner[0].Id == vehicle.Id, "Search by owner name failed.");

var initialVehicleHistory = await services.PromotionUsageService.GetUsageHistoryForVehicleAsync(vehicle.Id);
Assert(initialVehicleHistory.Count == 1 && initialVehicleHistory[0].Id == usage.Id, "Fetching vehicle usage history failed.");

var secondPromotion = await services.PromotionService.CreateAsync(new Promotion
{
    PromotionName = "Weekend Wash",
    Description = "Second promotion for same vehicle",
    IsActive = true
});

var secondPromotionEligibility = await services.PromotionUsageService.CheckEligibilityAsync(vehicle.Id, secondPromotion.Id);
Assert(secondPromotionEligibility.IsEligible, "The same vehicle should be eligible for a different promotion.");

var secondUsageResult = await services.PromotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
{
    VehicleNumberRaw = "CAB-1234",
    PhoneNumber = "0771234567",
    OwnerName = "Nimal Perera",
    Brand = "Toyota",
    Model = "Corolla",
    PromotionId = secondPromotion.Id,
    Mileage = 121000,
    AmountPaid = 6500m,
    ServiceDate = DateTime.UtcNow.AddMinutes(1)
});
Assert(secondUsageResult.IsSuccess, "Saving usage for a second promotion failed.");
Assert(secondUsageResult.Vehicle?.Id == vehicle.Id, "Second promotion usage should reuse the same vehicle.");
Assert(secondUsageResult.PromotionUsage is not null, "Second promotion usage result is missing the usage record.");

var historyAfterSecondPromotion = await services.PromotionUsageService.GetUsageHistoryForVehicleAsync(vehicle.Id);
Assert(historyAfterSecondPromotion.Count == 2, "Vehicle history should contain both promotion usages.");

var usageCountForFirstPromotion = await services.PromotionUsageService.GetUsageCountForPromotionAsync(promotion.Id);
Assert(usageCountForFirstPromotion == 1, "Usage count by first promotion failed.");

var usageCountForSecondPromotion = await services.PromotionUsageService.GetUsageCountForPromotionAsync(secondPromotion.Id);
Assert(usageCountForSecondPromotion == 1, "Usage count by second promotion failed.");

var activePromotions = await services.PromotionService.GetActiveAsync();
Assert(activePromotions.Any(item => item.Id == promotion.Id), "Active promotion lookup failed for the first promotion.");
Assert(activePromotions.Any(item => item.Id == secondPromotion.Id), "Active promotion lookup failed for the second promotion.");

var deactivatedPromotion = await services.PromotionService.DeactivateAsync(promotion.Id);
Assert(!deactivatedPromotion.IsActive, "Promotion deactivation failed.");

var inactivePromotionSaveResult = await services.PromotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
{
    VehicleNumberRaw = "KLM-9090",
    PhoneNumber = "0712345678",
    PromotionId = promotion.Id,
    ServiceDate = DateTime.UtcNow
});
Assert(!inactivePromotionSaveResult.IsSuccess && inactivePromotionSaveResult.Message == "This promotion is inactive.", "Inactive promotion rule failed.");

var oldPromotionHistory = await services.PromotionUsageService.SearchUsageRecordsAsync(new PromotionUsageRecordQuery
{
    PromotionId = promotion.Id
});
Assert(oldPromotionHistory.Count == 1 && oldPromotionHistory[0].PromotionUsageId == usage.Id, "History should remain visible after promotion deactivation.");

var secondPromotionHistory = await services.PromotionUsageService.SearchUsageRecordsAsync(new PromotionUsageRecordQuery
{
    SearchText = "Weekend Wash"
});
Assert(secondPromotionHistory.Count == 1 && secondPromotionHistory[0].PromotionId == secondPromotion.Id, "History search for the second promotion failed.");

var exportRecords = await services.PromotionUsageService.SearchUsageRecordsAsync(new PromotionUsageRecordQuery());
Assert(exportRecords.Count == 2, "Expected two history records before export.");

var exportResult = await services.ExportService.ExportPromotionUsageRecordsAsync(new ExportPromotionUsageRecordsRequest
{
    Records = exportRecords,
    ExportFolder = exportFolder,
    FileNamePrefix = "smoke-history"
});
Assert(exportResult.IsSuccess, "CSV export failed.");
Assert(exportResult.ExportedRecordCount == 2, "CSV export did not include the expected record count.");
Assert(exportResult.FilePath is not null && File.Exists(exportResult.FilePath), "CSV export file was not created.");
var exportContent = await File.ReadAllTextAsync(exportResult.FilePath!);
Assert(exportContent.Contains("Service Date", StringComparison.Ordinal), "CSV header row is missing.");
Assert(exportContent.Contains("New Year Promo", StringComparison.Ordinal), "CSV export content is missing the first promotion.");
Assert(exportContent.Contains("Weekend Wash", StringComparison.Ordinal), "CSV export content is missing the second promotion.");

var blockedExportTarget = Path.Combine(smokeDbDirectory, "blocked-export-target");
await File.WriteAllTextAsync(blockedExportTarget, "This file blocks folder creation.");
var exportFailure = await CaptureExceptionAsync(async () =>
{
    await services.ExportService.ExportPromotionUsageRecordsAsync(new ExportPromotionUsageRecordsRequest
    {
        Records = exportRecords,
        ExportFolder = blockedExportTarget,
        FileNamePrefix = "should-fail"
    });
});
Assert(exportFailure is not null, "Export to a blocked path should fail.");
DeleteIfExists(blockedExportTarget);

var passwordChangeResult = await services.SettingsService.ChangeDeletePasswordAsync(new ChangeDeletePasswordRequest
{
    CurrentPassword = "admin",
    NewPassword = "smoke-secret",
    ConfirmNewPassword = "smoke-secret"
});
Assert(passwordChangeResult.IsSuccess, "Changing the delete password failed.");

var restartedServices = CreateServices();
var restartedSettings = await restartedServices.SettingsService.GetAsync();
Assert(restartedSettings.ShopName == "PlateGuard Smoke Shop", "Settings did not persist after restart.");
Assert(restartedSettings.ExportFolder == exportFolder, "Export folder did not persist after restart.");

var restartedVehicle = await restartedServices.VehicleService.FindByVehicleNumberAsync("CAB-1234");
Assert(restartedVehicle?.Id == vehicle.Id, "Vehicle search after restart failed.");

var restartedHistory = await restartedServices.PromotionUsageService.SearchUsageRecordsAsync(new PromotionUsageRecordQuery());
Assert(restartedHistory.Count == 2, "History records did not persist after restart.");

var restartedPromotions = await restartedServices.PromotionService.GetAllAsync();
Assert(restartedPromotions.Count == 2, "Promotions did not persist after restart.");
Assert(restartedPromotions.Any(item => item.Id == promotion.Id && !item.IsActive), "First promotion should still be inactive after restart.");
Assert(restartedPromotions.Any(item => item.Id == secondPromotion.Id && item.IsActive), "Second promotion should remain active after restart.");

var deleteOldPassword = await restartedServices.PromotionUsageService.DeleteUsageAsync(usage.Id, "admin");
Assert(!deleteOldPassword.IsSuccess, "Delete should fail with the previous password after a password change.");

var deleteCorrectPassword = await restartedServices.PromotionUsageService.DeleteUsageAsync(usage.Id, "smoke-secret");
Assert(deleteCorrectPassword.IsSuccess, "Delete should succeed with the updated password.");

var eligibilityAfterDelete = await restartedServices.PromotionUsageService.CheckEligibilityAsync(vehicle.Id, promotion.Id);
Assert(!await restartedServices.PromotionUsageRepository.ExistsAsync(vehicle.Id, promotion.Id), "Deleting the first usage should remove the vehicle/promotion pair.");
Assert(!eligibilityAfterDelete.IsEligible && eligibilityAfterDelete.Message == "This promotion is inactive.", "After delete, the first promotion should only be blocked by its inactive status.");

var secondPromotionAfterDelete = await restartedServices.PromotionUsageService.CheckEligibilityAsync(vehicle.Id, secondPromotion.Id);
Assert(!secondPromotionAfterDelete.IsEligible, "Deleting the first usage should not affect the second promotion usage.");

var vehicleHistoryAfterDelete = await restartedServices.PromotionUsageService.GetUsageHistoryForVehicleAsync(vehicle.Id);
Assert(vehicleHistoryAfterDelete.Count == 1, "Vehicle history should retain the remaining promotion usage after delete.");

Console.WriteLine($"Smoke DB: {smokeDbPath}");
Console.WriteLine($"Settings seeded: {settings.Id}");
Console.WriteLine($"Settings updated: {settingsUpdateResult.Settings?.ShopName} -> {settingsUpdateResult.Settings?.ExportFolder}");
Console.WriteLine($"Unknown vehicle search empty: {unknownVehicle is null}");
Console.WriteLine($"Validation checks passed: blank vehicle, blank phone, missing promotion");
Console.WriteLine($"Vehicle created: {vehicle.VehicleNumberNormalized} ({vehicle.OwnerName})");
Console.WriteLine($"Promotions created: {promotion.PromotionName}, {secondPromotion.PromotionName}");
Console.WriteLine($"Duplicate blocked: {!duplicateResult.IsSuccess}");
Console.WriteLine($"Inactive promotion blocked: {!inactivePromotionSaveResult.IsSuccess}");
Console.WriteLine($"Phone search results: {byPhone.Count}");
Console.WriteLine($"Owner search results: {byOwner.Count}");
Console.WriteLine($"Export created: {exportResult.FilePath}");
Console.WriteLine($"Blocked export failed as expected: {exportFailure is not null}");
Console.WriteLine($"Password changed: {passwordChangeResult.IsSuccess}");
Console.WriteLine($"Restart persistence passed: {restartedHistory.Count} record(s), {restartedPromotions.Count} promotion(s)");
Console.WriteLine($"Delete old password blocked: {!deleteOldPassword.IsSuccess}");
Console.WriteLine($"Delete correct password succeeded: {deleteCorrectPassword.IsSuccess}");
Console.WriteLine("Smoke tests passed.");

static TestServices CreateServices()
{
    IVehicleRepository vehicleRepository = new VehicleRepository();
    IPromotionRepository promotionRepository = new PromotionRepository();
    IPromotionUsageRepository promotionUsageRepository = new PromotionUsageRepository();
    ISettingsRepository settingsRepository = new SettingsRepository();

    IVehicleService vehicleService = new VehicleService(vehicleRepository);
    IPromotionService promotionService = new PromotionService(promotionRepository);
    IPromotionUsageService promotionUsageService = new PromotionUsageService(
        vehicleRepository,
        promotionRepository,
        promotionUsageRepository,
        settingsRepository);
    ISettingsService settingsService = new SettingsService(settingsRepository);
    IExportService exportService = new ExportService(settingsRepository);

    return new TestServices(
        vehicleRepository,
        promotionRepository,
        promotionUsageRepository,
        settingsRepository,
        vehicleService,
        promotionService,
        promotionUsageService,
        settingsService,
        exportService);
}

static async Task<Exception?> CaptureExceptionAsync(Func<Task> action)
{
    try
    {
        await action();
        return null;
    }
    catch (Exception exception)
    {
        return exception;
    }
}

static async Task WaitUntilAsync(Func<bool> condition, string message, int timeoutMs = 5000, int pollMs = 50)
{
    var startedAt = DateTime.UtcNow;
    while ((DateTime.UtcNow - startedAt).TotalMilliseconds < timeoutMs)
    {
        if (condition())
        {
            return;
        }

        await Task.Delay(pollMs);
    }

    throw new InvalidOperationException(message);
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void DeleteIfExists(string path)
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }
}

internal sealed record TestServices(
    IVehicleRepository VehicleRepository,
    IPromotionRepository PromotionRepository,
    IPromotionUsageRepository PromotionUsageRepository,
    ISettingsRepository SettingsRepository,
    IVehicleService VehicleService,
    IPromotionService PromotionService,
    IPromotionUsageService PromotionUsageService,
    ISettingsService SettingsService,
    IExportService ExportService);
