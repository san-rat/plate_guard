using PlateGuard.Core.Helpers;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;

namespace PlateGuard.Core.Services;

public sealed class VehicleService(IVehicleRepository vehicleRepository) : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository = vehicleRepository;

    public Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _vehicleRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Vehicle>> SearchByPhoneNumberAsync(string phoneNumberQuery, CancellationToken cancellationToken = default)
    {
        return _vehicleRepository.SearchByPhoneNumberAsync(phoneNumberQuery, cancellationToken);
    }

    public Task<IReadOnlyList<Vehicle>> SearchByOwnerNameAsync(string ownerNameQuery, CancellationToken cancellationToken = default)
    {
        return _vehicleRepository.SearchByOwnerNameAsync(ownerNameQuery, cancellationToken);
    }

    public Task<Vehicle?> FindByVehicleNumberAsync(string vehicleNumber, CancellationToken cancellationToken = default)
    {
        return _vehicleRepository.GetByNormalizedNumberAsync(vehicleNumber, cancellationToken);
    }

    public async Task<Vehicle> CreateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        ValidateVehicle(vehicle);
        NormalizeVehicle(vehicle);
        vehicle.CreatedAt = vehicle.CreatedAt == default ? DateTime.UtcNow : vehicle.CreatedAt;

        return await _vehicleRepository.AddAsync(vehicle, cancellationToken);
    }

    public async Task<Vehicle> UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        if (vehicle.Id <= 0)
        {
            throw new ArgumentException("Vehicle id is required.", nameof(vehicle));
        }

        ValidateVehicle(vehicle);
        NormalizeVehicle(vehicle);
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);
        return vehicle;
    }

    private static void ValidateVehicle(Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.VehicleNumberRaw))
        {
            throw new ArgumentException("Vehicle number is required.", nameof(vehicle));
        }

        if (string.IsNullOrWhiteSpace(vehicle.PhoneNumber))
        {
            throw new ArgumentException("Phone number is required.", nameof(vehicle));
        }
    }

    private static void NormalizeVehicle(Vehicle vehicle)
    {
        vehicle.VehicleNumberRaw = vehicle.VehicleNumberRaw.Trim();
        vehicle.VehicleNumberNormalized = VehicleNumberNormalizer.Normalize(vehicle.VehicleNumberRaw);
        vehicle.PhoneNumber = vehicle.PhoneNumber.Trim();
        vehicle.OwnerName = string.IsNullOrWhiteSpace(vehicle.OwnerName) ? null : vehicle.OwnerName.Trim();
        vehicle.Brand = string.IsNullOrWhiteSpace(vehicle.Brand) ? null : vehicle.Brand.Trim();
        vehicle.Model = string.IsNullOrWhiteSpace(vehicle.Model) ? null : vehicle.Model.Trim();
    }
}
