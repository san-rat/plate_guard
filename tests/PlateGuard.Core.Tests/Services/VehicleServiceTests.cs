using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Core.Services;

namespace PlateGuard.Core.Tests.Services;

public sealed class VehicleServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesAndTrimsVehicleFields()
    {
        var repository = new CapturingVehicleRepository();
        var service = new VehicleService(repository);

        var createdVehicle = await service.CreateAsync(new Vehicle
        {
            VehicleNumberRaw = " csa-4653 ",
            PhoneNumber = " 0771234567 ",
            OwnerName = " Jane Doe ",
            Brand = " Toyota ",
            Model = " Axio "
        });

        Assert.NotNull(repository.AddedVehicle);
        Assert.Equal("csa-4653", repository.AddedVehicle!.VehicleNumberRaw);
        Assert.Equal("CSA4653", repository.AddedVehicle.VehicleNumberNormalized);
        Assert.Equal("0771234567", repository.AddedVehicle.PhoneNumber);
        Assert.Equal("Jane Doe", repository.AddedVehicle.OwnerName);
        Assert.Equal("Toyota", repository.AddedVehicle.Brand);
        Assert.Equal("Axio", repository.AddedVehicle.Model);
        Assert.NotEqual(default, repository.AddedVehicle.CreatedAt);
        Assert.Same(repository.AddedVehicle, createdVehicle);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenPhoneNumberIsMissing()
    {
        var repository = new CapturingVehicleRepository();
        var service = new VehicleService(repository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new Vehicle
        {
            VehicleNumberRaw = "CSA-4653",
            PhoneNumber = "   "
        }));

        Assert.Equal("vehicle", exception.ParamName);
        Assert.Null(repository.AddedVehicle);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAtAndNormalizesOptionalFields()
    {
        var repository = new CapturingVehicleRepository();
        var service = new VehicleService(repository);
        var vehicle = new Vehicle
        {
            Id = 7,
            VehicleNumberRaw = " ab-1234 ",
            PhoneNumber = " 0710000000 ",
            OwnerName = " John ",
            Brand = " ",
            Model = " Fit "
        };

        var updatedVehicle = await service.UpdateAsync(vehicle);

        Assert.Same(vehicle, updatedVehicle);
        Assert.NotNull(repository.UpdatedVehicle);
        Assert.Equal("ab-1234", repository.UpdatedVehicle!.VehicleNumberRaw);
        Assert.Equal("AB1234", repository.UpdatedVehicle.VehicleNumberNormalized);
        Assert.Equal("0710000000", repository.UpdatedVehicle.PhoneNumber);
        Assert.Equal("John", repository.UpdatedVehicle.OwnerName);
        Assert.Null(repository.UpdatedVehicle.Brand);
        Assert.Equal("Fit", repository.UpdatedVehicle.Model);
        Assert.NotNull(repository.UpdatedVehicle.UpdatedAt);
    }

    private sealed class CapturingVehicleRepository : IVehicleRepository
    {
        public Vehicle? AddedVehicle { get; private set; }
        public Vehicle? UpdatedVehicle { get; private set; }

        public Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Vehicle?>(null);
        public Task<Vehicle?> GetByNormalizedNumberAsync(string vehicleNumber, CancellationToken cancellationToken = default) => Task.FromResult<Vehicle?>(null);
        public Task<IReadOnlyList<Vehicle>> SearchByPhoneNumberAsync(string phoneNumberQuery, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Vehicle>>([]);
        public Task<IReadOnlyList<Vehicle>> SearchByOwnerNameAsync(string ownerNameQuery, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Vehicle>>([]);

        public Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
        {
            AddedVehicle = vehicle;
            return Task.FromResult(vehicle);
        }

        public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
        {
            UpdatedVehicle = vehicle;
            return Task.CompletedTask;
        }
    }
}
