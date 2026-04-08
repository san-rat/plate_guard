using PlateGuard.Core.Models;

namespace PlateGuard.Core.Interfaces;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Vehicle?> GetByNormalizedNumberAsync(string vehicleNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> SearchByPhoneNumberAsync(string phoneNumberQuery, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> SearchByOwnerNameAsync(string ownerNameQuery, CancellationToken cancellationToken = default);
    Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}
