using PlateGuard.Core.Helpers;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;

namespace PlateGuard.Core.Services;

public sealed class PromotionUsageService(
    IVehicleRepository vehicleRepository,
    IPromotionRepository promotionRepository,
    IPromotionUsageRepository promotionUsageRepository,
    ISettingsRepository settingsRepository) : IPromotionUsageService
{
    private readonly IVehicleRepository _vehicleRepository = vehicleRepository;
    private readonly IPromotionRepository _promotionRepository = promotionRepository;
    private readonly IPromotionUsageRepository _promotionUsageRepository = promotionUsageRepository;
    private readonly ISettingsRepository _settingsRepository = settingsRepository;

    public async Task<EligibilityCheckResult> CheckEligibilityAsync(string vehicleNumber, int promotionId, CancellationToken cancellationToken = default)
    {
        var normalizedVehicleNumber = VehicleNumberNormalizer.Normalize(vehicleNumber);
        var vehicle = string.IsNullOrEmpty(normalizedVehicleNumber)
            ? null
            : await _vehicleRepository.GetByNormalizedNumberAsync(normalizedVehicleNumber, cancellationToken);

        return await CheckEligibilityAsync(vehicle, promotionId, cancellationToken);
    }

    public async Task<EligibilityCheckResult> CheckEligibilityAsync(int vehicleId, int promotionId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
        return await CheckEligibilityAsync(vehicle, promotionId, cancellationToken);
    }

    public Task<IReadOnlyList<PromotionUsage>> GetUsageHistoryForVehicleAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        return _promotionUsageRepository.GetByVehicleIdAsync(vehicleId, cancellationToken);
    }

    public Task<int> GetUsageCountForPromotionAsync(int promotionId, CancellationToken cancellationToken = default)
    {
        return _promotionUsageRepository.CountByPromotionIdAsync(promotionId, cancellationToken);
    }

    public async Task<SavePromotionUsageResult> SaveVehicleAndUsageAsync(SavePromotionUsageRequest request, CancellationToken cancellationToken = default)
    {
        var validationMessage = ValidateSaveRequest(request);
        if (validationMessage is not null)
        {
            return new SavePromotionUsageResult
            {
                IsSuccess = false,
                Message = validationMessage
            };
        }

        var promotion = await _promotionRepository.GetByIdAsync(request.PromotionId, cancellationToken);
        if (promotion is null)
        {
            return new SavePromotionUsageResult
            {
                IsSuccess = false,
                Message = "Selected promotion was not found."
            };
        }

        var existingVehicle = await _vehicleRepository.GetByNormalizedNumberAsync(request.VehicleNumberRaw, cancellationToken);
        var eligibility = await CheckEligibilityAsync(existingVehicle, promotion.Id, cancellationToken);
        if (!eligibility.IsEligible)
        {
            return new SavePromotionUsageResult
            {
                IsSuccess = false,
                Message = eligibility.Message,
                Vehicle = existingVehicle,
                PromotionUsage = eligibility.ExistingUsage
            };
        }

        var createdNewVehicle = existingVehicle is null;
        var vehicle = createdNewVehicle
            ? await _vehicleRepository.AddAsync(CreateVehicle(request), cancellationToken)
            : await UpdateVehicleAsync(existingVehicle!, request, cancellationToken);

        var promotionUsage = new PromotionUsage
        {
            VehicleId = vehicle.Id,
            PromotionId = promotion.Id,
            ServiceDate = request.ServiceDate ?? DateTime.UtcNow,
            Mileage = request.Mileage,
            NormalPrice = request.NormalPrice,
            DiscountedPrice = request.DiscountedPrice,
            AmountPaid = request.AmountPaid,
            Notes = request.Notes
        };

        var savedUsage = await _promotionUsageRepository.AddAsync(promotionUsage, cancellationToken);

        return new SavePromotionUsageResult
        {
            IsSuccess = true,
            Message = "Record saved successfully.",
            Vehicle = vehicle,
            PromotionUsage = savedUsage,
            CreatedNewVehicle = createdNewVehicle
        };
    }

    public async Task<OperationResult> UpdateUsageAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default)
    {
        if (promotionUsage.Id <= 0)
        {
            return Failure("Promotion usage id is required.");
        }

        var existingUsage = await _promotionUsageRepository.GetByIdAsync(promotionUsage.Id, cancellationToken);
        if (existingUsage is null)
        {
            return Failure("Promotion usage record was not found.");
        }

        var promotion = await _promotionRepository.GetByIdAsync(promotionUsage.PromotionId, cancellationToken);
        if (promotion is null)
        {
            return Failure("Selected promotion was not found.");
        }

        if (!promotion.IsActive && existingUsage.PromotionId != promotionUsage.PromotionId)
        {
            return Failure("This promotion is inactive.");
        }

        var usagePairChanged = existingUsage.VehicleId != promotionUsage.VehicleId || existingUsage.PromotionId != promotionUsage.PromotionId;
        if (usagePairChanged && await _promotionUsageRepository.ExistsAsync(promotionUsage.VehicleId, promotionUsage.PromotionId, cancellationToken))
        {
            return Failure("This vehicle has already used the selected promotion.");
        }

        await _promotionUsageRepository.UpdateAsync(promotionUsage, cancellationToken);

        return Success("Record updated successfully.");
    }

    public async Task<OperationResult> DeleteUsageAsync(int id, string deletePassword, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Failure("Promotion usage id is required.");
        }

        var settings = await _settingsRepository.GetAsync(cancellationToken);
        if (settings is null)
        {
            return Failure("Delete password is not configured.");
        }

        if (!DeletePasswordHasher.Verify(deletePassword, settings.DeletePasswordHash))
        {
            return Failure("Incorrect delete password.");
        }

        await _promotionUsageRepository.DeleteAsync(id, cancellationToken);

        return Success("Record deleted successfully.");
    }

    private async Task<EligibilityCheckResult> CheckEligibilityAsync(Vehicle? vehicle, int promotionId, CancellationToken cancellationToken)
    {
        if (promotionId <= 0)
        {
            return new EligibilityCheckResult
            {
                IsEligible = false,
                Message = "Promotion is required.",
                Vehicle = vehicle
            };
        }

        var promotion = await _promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion is null)
        {
            return new EligibilityCheckResult
            {
                IsEligible = false,
                Message = "Selected promotion was not found.",
                Vehicle = vehicle
            };
        }

        if (vehicle is not null)
        {
            var existingUsage = await FindExistingUsageAsync(vehicle.Id, promotion.Id, cancellationToken);
            if (existingUsage is not null)
            {
                return new EligibilityCheckResult
                {
                    IsEligible = false,
                    Message = "Promotion already used for this vehicle.",
                    Vehicle = vehicle,
                    Promotion = promotion,
                    ExistingUsage = existingUsage
                };
            }
        }

        if (!promotion.IsActive)
        {
            return new EligibilityCheckResult
            {
                IsEligible = false,
                Message = "This promotion is inactive.",
                Vehicle = vehicle,
                Promotion = promotion
            };
        }

        return new EligibilityCheckResult
        {
            IsEligible = true,
            Message = "Vehicle is eligible for this promotion.",
            Vehicle = vehicle,
            Promotion = promotion
        };
    }

    private async Task<PromotionUsage?> FindExistingUsageAsync(int vehicleId, int promotionId, CancellationToken cancellationToken)
    {
        var usageHistory = await _promotionUsageRepository.GetByVehicleIdAsync(vehicleId, cancellationToken);
        return usageHistory.FirstOrDefault(usage => usage.PromotionId == promotionId);
    }

    private async Task<Vehicle> UpdateVehicleAsync(Vehicle existingVehicle, SavePromotionUsageRequest request, CancellationToken cancellationToken)
    {
        existingVehicle.VehicleNumberRaw = request.VehicleNumberRaw.Trim();
        existingVehicle.PhoneNumber = request.PhoneNumber.Trim();
        existingVehicle.OwnerName = string.IsNullOrWhiteSpace(request.OwnerName) ? existingVehicle.OwnerName : request.OwnerName.Trim();
        existingVehicle.Brand = string.IsNullOrWhiteSpace(request.Brand) ? existingVehicle.Brand : request.Brand.Trim();
        existingVehicle.Model = string.IsNullOrWhiteSpace(request.Model) ? existingVehicle.Model : request.Model.Trim();

        await _vehicleRepository.UpdateAsync(existingVehicle, cancellationToken);
        return existingVehicle;
    }

    private static Vehicle CreateVehicle(SavePromotionUsageRequest request)
    {
        return new Vehicle
        {
            VehicleNumberRaw = request.VehicleNumberRaw.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            OwnerName = request.OwnerName?.Trim(),
            Brand = request.Brand?.Trim(),
            Model = request.Model?.Trim()
        };
    }

    private static string? ValidateSaveRequest(SavePromotionUsageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VehicleNumberRaw))
        {
            return "Vehicle number is required.";
        }

        if (request.PromotionId <= 0)
        {
            return "Promotion is required.";
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return "Phone number is required.";
        }

        if (request.Mileage < 0)
        {
            return "Mileage cannot be negative.";
        }

        if (request.NormalPrice < 0)
        {
            return "Normal price cannot be negative.";
        }

        if (request.DiscountedPrice < 0)
        {
            return "Discounted price cannot be negative.";
        }

        if (request.AmountPaid < 0)
        {
            return "Amount paid cannot be negative.";
        }

        return null;
    }

    private static OperationResult Success(string message) => new() { IsSuccess = true, Message = message };
    private static OperationResult Failure(string message) => new() { IsSuccess = false, Message = message };
}
