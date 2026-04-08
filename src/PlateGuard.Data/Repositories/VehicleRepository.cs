using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Helpers;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Data.Db;
using PlateGuard.Data.Mappers;

namespace PlateGuard.Data.Repositories;

public sealed class VehicleRepository() : RepositoryBase(new PlateGuardDbContextFactory()), IVehicleRepository
{
    public async Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        var entity = await dbContext.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

        return entity is null ? null : VehicleMapper.ToModel(entity);
    }

    public async Task<Vehicle?> GetByNormalizedNumberAsync(string vehicleNumber, CancellationToken cancellationToken = default)
    {
        var normalizedVehicleNumber = VehicleNumberNormalizer.Normalize(vehicleNumber);
        if (string.IsNullOrEmpty(normalizedVehicleNumber))
        {
            return null;
        }

        await using var dbContext = CreateDbContext();
        var entity = await dbContext.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(vehicle => vehicle.VehicleNumberNormalized == normalizedVehicleNumber, cancellationToken);

        return entity is null ? null : VehicleMapper.ToModel(entity);
    }

    public async Task<IReadOnlyList<Vehicle>> SearchByPhoneNumberAsync(string phoneNumberQuery, CancellationToken cancellationToken = default)
    {
        var query = phoneNumberQuery?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        await using var dbContext = CreateDbContext();
        var entities = await dbContext.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.PhoneNumber.Contains(query))
            .OrderBy(vehicle => vehicle.VehicleNumberNormalized)
            .ToListAsync(cancellationToken);

        return entities.Select(VehicleMapper.ToModel).ToList();
    }

    public async Task<IReadOnlyList<Vehicle>> SearchByOwnerNameAsync(string ownerNameQuery, CancellationToken cancellationToken = default)
    {
        var query = ownerNameQuery?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        await using var dbContext = CreateDbContext();
        var entities = await dbContext.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.OwnerName != null && EF.Functions.Like(vehicle.OwnerName, $"%{query}%"))
            .OrderBy(vehicle => vehicle.OwnerName)
            .ThenBy(vehicle => vehicle.VehicleNumberNormalized)
            .ToListAsync(cancellationToken);

        return entities.Select(VehicleMapper.ToModel).ToList();
    }

    public async Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var entity = VehicleMapper.ToEntity(vehicle);
        NormalizeVehicle(entity);
        entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;

        await dbContext.Vehicles.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return VehicleMapper.ToModel(entity);
    }

    public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var entity = await dbContext.Vehicles.FirstOrDefaultAsync(existing => existing.Id == vehicle.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Vehicle with id {vehicle.Id} was not found.");

        vehicle.UpdatedAt = DateTime.UtcNow;
        VehicleMapper.UpdateEntity(entity, vehicle);
        NormalizeVehicle(entity);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void NormalizeVehicle(PlateGuard.Data.Entities.VehicleEntity entity)
    {
        var sourceValue = string.IsNullOrWhiteSpace(entity.VehicleNumberNormalized)
            ? entity.VehicleNumberRaw
            : entity.VehicleNumberNormalized;

        entity.VehicleNumberNormalized = VehicleNumberNormalizer.Normalize(sourceValue);
    }
}
