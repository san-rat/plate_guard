using PlateGuard.Core.Models;

namespace PlateGuard.Core.Interfaces;

public interface IVehicleService
{
    Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Vehicle?> FindByVehicleNumberAsync(string vehicleNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> SearchByPhoneNumberAsync(string phoneNumberQuery, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> SearchByOwnerNameAsync(string ownerNameQuery, CancellationToken cancellationToken = default);
    Task<Vehicle> CreateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task<Vehicle> UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}
