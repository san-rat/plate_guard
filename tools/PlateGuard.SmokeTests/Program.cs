using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
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

var settings = await settingsRepository.GetAsync() ?? throw new InvalidOperationException("Expected seeded settings row.");
Assert(settings.Id == PlateGuardDatabaseInitializer.DefaultSettingsId, "Default settings row was not seeded.");

var vehicle = await vehicleRepository.AddAsync(new Vehicle
{
    VehicleNumberRaw = "cab-1234",
    PhoneNumber = "0771234567",
    OwnerName = "Nimal Perera",
    Brand = "Toyota",
    Model = "Corolla"
});

Assert(vehicle.VehicleNumberNormalized == "CAB1234", "Vehicle number normalization did not persist correctly.");

var promotion = await promotionRepository.AddAsync(new Promotion
{
    PromotionName = "New Year Promo",
    Description = "Smoke test promotion",
    IsActive = true
});

var usage = await promotionUsageRepository.AddAsync(new PromotionUsage
{
    VehicleId = vehicle.Id,
    PromotionId = promotion.Id,
    Mileage = 120000,
    AmountPaid = 5000m,
    ServiceDate = DateTime.UtcNow
});

Assert(await promotionUsageRepository.ExistsAsync(vehicle.Id, promotion.Id), "Promotion usage existence check failed.");

var duplicateBlocked = false;
try
{
    await promotionUsageRepository.AddAsync(new PromotionUsage
    {
        VehicleId = vehicle.Id,
        PromotionId = promotion.Id,
        ServiceDate = DateTime.UtcNow
    });
}
catch (DbUpdateException)
{
    duplicateBlocked = true;
}

Assert(duplicateBlocked, "Duplicate promotion usage was not blocked.");

var byPlate = await vehicleRepository.GetByNormalizedNumberAsync("CAB 1234");
Assert(byPlate?.Id == vehicle.Id, "Search by normalized plate failed.");

var byPhone = await vehicleRepository.SearchByPhoneNumberAsync("0771");
Assert(byPhone.Count == 1 && byPhone[0].Id == vehicle.Id, "Search by phone number failed.");

var byOwner = await vehicleRepository.SearchByOwnerNameAsync("Nimal");
Assert(byOwner.Count == 1 && byOwner[0].Id == vehicle.Id, "Search by owner name failed.");

var usages = await promotionUsageRepository.GetByVehicleIdAsync(vehicle.Id);
Assert(usages.Count == 1 && usages[0].Id == usage.Id, "Fetching vehicle usage history failed.");

var activePromotions = await promotionRepository.GetActiveAsync();
Assert(activePromotions.Any(item => item.Id == promotion.Id), "Active promotion lookup failed.");

Console.WriteLine($"Smoke DB: {smokeDbPath}");
Console.WriteLine($"Settings seeded: {settings.Id}");
Console.WriteLine($"Vehicle created: {vehicle.VehicleNumberNormalized} ({vehicle.OwnerName})");
Console.WriteLine($"Promotion created: {promotion.PromotionName}");
Console.WriteLine($"Usage created: VehicleId={usage.VehicleId}, PromotionId={usage.PromotionId}");
Console.WriteLine($"Duplicate blocked: {duplicateBlocked}");
Console.WriteLine($"Phone search results: {byPhone.Count}");
Console.WriteLine($"Owner search results: {byOwner.Count}");
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
