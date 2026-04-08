using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Core.Services;
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

var settings = await settingsRepository.GetAsync() ?? throw new InvalidOperationException("Expected seeded settings row.");
Assert(settings.Id == PlateGuardDatabaseInitializer.DefaultSettingsId, "Default settings row was not seeded.");
Assert(settings.DeletePasswordHash is { Length: 64 }, "Seeded delete password hash is invalid.");

var promotion = await promotionService.CreateAsync(new Promotion
{
    PromotionName = "New Year Promo",
    Description = "Smoke test promotion",
    IsActive = true
});

var eligibilityBeforeSave = await promotionUsageService.CheckEligibilityAsync("cab-1234", promotion.Id);
Assert(eligibilityBeforeSave.IsEligible, "Vehicle should be eligible before any usage is saved.");

var saveResult = await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
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
Assert(await promotionUsageRepository.ExistsAsync(vehicle.Id, promotion.Id), "Promotion usage existence check failed.");

var eligibilityAfterSave = await promotionUsageService.CheckEligibilityAsync(vehicle.Id, promotion.Id);
Assert(!eligibilityAfterSave.IsEligible, "Vehicle should not be eligible after usage is saved.");
Assert(eligibilityAfterSave.ExistingUsage?.Id == usage.Id, "Eligibility check did not return the existing usage.");

var duplicateResult = await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
{
    VehicleNumberRaw = "CAB 1234",
    PhoneNumber = "0771234567",
    PromotionId = promotion.Id,
    ServiceDate = DateTime.UtcNow
});

Assert(!duplicateResult.IsSuccess, "Duplicate promotion usage was not blocked.");

var byPlate = await vehicleService.FindByVehicleNumberAsync("CAB 1234");
Assert(byPlate?.Id == vehicle.Id, "Search by normalized plate failed.");

var byPhone = await vehicleService.SearchByPhoneNumberAsync("0771");
Assert(byPhone.Count == 1 && byPhone[0].Id == vehicle.Id, "Search by phone number failed.");

var byOwner = await vehicleService.SearchByOwnerNameAsync("Nimal");
Assert(byOwner.Count == 1 && byOwner[0].Id == vehicle.Id, "Search by owner name failed.");

var usages = await promotionUsageService.GetUsageHistoryForVehicleAsync(vehicle.Id);
Assert(usages.Count == 1 && usages[0].Id == usage.Id, "Fetching vehicle usage history failed.");

var usageCount = await promotionUsageService.GetUsageCountForPromotionAsync(promotion.Id);
Assert(usageCount == 1, "Usage count by promotion failed.");

var activePromotions = await promotionService.GetActiveAsync();
Assert(activePromotions.Any(item => item.Id == promotion.Id), "Active promotion lookup failed.");

var deleteWrongPassword = await promotionUsageService.DeleteUsageAsync(usage.Id, "wrong-password");
Assert(!deleteWrongPassword.IsSuccess, "Delete should fail with an incorrect password.");

var deleteCorrectPassword = await promotionUsageService.DeleteUsageAsync(usage.Id, "admin");
Assert(deleteCorrectPassword.IsSuccess, "Delete should succeed with the correct password.");

var eligibilityAfterDelete = await promotionUsageService.CheckEligibilityAsync(vehicle.Id, promotion.Id);
Assert(eligibilityAfterDelete.IsEligible, "Vehicle should become eligible again after usage deletion.");

Console.WriteLine($"Smoke DB: {smokeDbPath}");
Console.WriteLine($"Settings seeded: {settings.Id}");
Console.WriteLine($"Vehicle created: {vehicle.VehicleNumberNormalized} ({vehicle.OwnerName})");
Console.WriteLine($"Promotion created: {promotion.PromotionName}");
Console.WriteLine($"Usage created: VehicleId={usage.VehicleId}, PromotionId={usage.PromotionId}");
Console.WriteLine($"Duplicate blocked: {!duplicateResult.IsSuccess}");
Console.WriteLine($"Phone search results: {byPhone.Count}");
Console.WriteLine($"Owner search results: {byOwner.Count}");
Console.WriteLine($"Delete wrong password blocked: {!deleteWrongPassword.IsSuccess}");
Console.WriteLine($"Delete correct password succeeded: {deleteCorrectPassword.IsSuccess}");
Console.WriteLine("Smoke tests passed.");

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
