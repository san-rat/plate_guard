using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlateGuard.Core.Helpers;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Data.Db;
using PlateGuard.Data.Entities;
using PlateGuard.Data.Mappers;

namespace PlateGuard.Data.Repositories;

public sealed class PromotionUsageTransactionalWriter()
    : RepositoryBase(new PlateGuardDbContextFactory()), IPromotionUsageTransactionalWriter
{
    public async Task<SavePromotionUsageResult> SaveVehicleAndUsageAsync(
        Vehicle? existingVehicle,
        SavePromotionUsageRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var vehicleEntity = await GetVehicleEntityForSaveAsync(dbContext, existingVehicle, request, cancellationToken);
            if (vehicleEntity is null)
            {
                return Failure("Vehicle record was not found.");
            }

            var usageEntity = new PromotionUsageEntity
            {
                Vehicle = vehicleEntity,
                PromotionId = request.PromotionId,
                ServiceDate = (request.ServiceDate ?? DateTime.Today).Date,
                Mileage = request.Mileage,
                NormalPrice = request.NormalPrice,
                DiscountedPrice = request.DiscountedPrice,
                AmountPaid = request.AmountPaid,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.PromotionUsages.AddAsync(usageEntity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new SavePromotionUsageResult
            {
                IsSuccess = true,
                Message = "Record saved successfully.",
                Vehicle = VehicleMapper.ToModel(vehicleEntity),
                PromotionUsage = PromotionUsageMapper.ToModel(usageEntity),
                CreatedNewVehicle = existingVehicle is null
            };
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure(MapDbUpdateFailure(
                exception,
                "Could not save the record because the database rejected the change."));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<OperationResult> UpdateUsageRecordAsync(
        Vehicle vehicle,
        PromotionUsage promotionUsage,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var usageEntity = await dbContext.PromotionUsages
                .FirstOrDefaultAsync(existing => existing.Id == promotionUsage.Id, cancellationToken);
            if (usageEntity is null)
            {
                return FailureResult("Promotion usage record was not found.");
            }

            var vehicleEntity = await dbContext.Vehicles
                .FirstOrDefaultAsync(existing => existing.Id == vehicle.Id, cancellationToken);
            if (vehicleEntity is null)
            {
                return FailureResult("Vehicle record was not found.");
            }

            promotionUsage.ServiceDate = promotionUsage.ServiceDate.Date;
            promotionUsage.UpdatedAt = DateTime.UtcNow;
            PromotionUsageMapper.UpdateEntity(usageEntity, promotionUsage);

            vehicle.UpdatedAt = DateTime.UtcNow;
            VehicleMapper.UpdateEntity(vehicleEntity, vehicle);
            NormalizeVehicle(vehicleEntity);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Success("Record updated successfully.");
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return FailureResult(MapDbUpdateFailure(
                exception,
                "Could not update the record because the database rejected the change."));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<VehicleEntity?> GetVehicleEntityForSaveAsync(
        PlateGuardDbContext dbContext,
        Vehicle? existingVehicle,
        SavePromotionUsageRequest request,
        CancellationToken cancellationToken)
    {
        if (existingVehicle is null)
        {
            var entity = new VehicleEntity
            {
                VehicleNumberRaw = request.VehicleNumberRaw.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                OwnerName = NormalizeOptionalText(request.OwnerName),
                Brand = NormalizeOptionalText(request.Brand),
                Model = NormalizeOptionalText(request.Model),
                CreatedAt = DateTime.UtcNow
            };

            NormalizeVehicle(entity);
            await dbContext.Vehicles.AddAsync(entity, cancellationToken);
            return entity;
        }

        var trackedVehicle = await dbContext.Vehicles
            .FirstOrDefaultAsync(vehicle => vehicle.Id == existingVehicle.Id, cancellationToken);
        if (trackedVehicle is null)
        {
            return null;
        }

        trackedVehicle.VehicleNumberRaw = request.VehicleNumberRaw.Trim();
        trackedVehicle.PhoneNumber = request.PhoneNumber.Trim();
        trackedVehicle.OwnerName = string.IsNullOrWhiteSpace(request.OwnerName)
            ? trackedVehicle.OwnerName
            : request.OwnerName.Trim();
        trackedVehicle.Brand = string.IsNullOrWhiteSpace(request.Brand)
            ? trackedVehicle.Brand
            : request.Brand.Trim();
        trackedVehicle.Model = string.IsNullOrWhiteSpace(request.Model)
            ? trackedVehicle.Model
            : request.Model.Trim();
        trackedVehicle.UpdatedAt = DateTime.UtcNow;

        NormalizeVehicle(trackedVehicle);
        return trackedVehicle;
    }

    private static void NormalizeVehicle(VehicleEntity entity)
    {
        entity.VehicleNumberNormalized = VehicleNumberNormalizer.Normalize(entity.VehicleNumberRaw);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string MapDbUpdateFailure(DbUpdateException exception, string fallbackMessage)
    {
        if (exception.InnerException is SqliteException sqliteException)
        {
            var message = sqliteException.Message;
            if (message.Contains("PromotionUsages.VehicleId, PromotionUsages.PromotionId", StringComparison.Ordinal))
            {
                return "This vehicle has already used the selected promotion.";
            }

            if (message.Contains("Vehicles.VehicleNumberNormalized", StringComparison.Ordinal))
            {
                return "Vehicle number already exists. Search again and retry.";
            }
        }

        return fallbackMessage;
    }

    private static SavePromotionUsageResult Failure(string message)
    {
        return new SavePromotionUsageResult
        {
            IsSuccess = false,
            Message = message
        };
    }

    private static OperationResult Success(string message) => new() { IsSuccess = true, Message = message };
    private static OperationResult FailureResult(string message) => new() { IsSuccess = false, Message = message };
}
