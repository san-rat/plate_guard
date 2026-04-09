using PlateGuard.Core.Helpers;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Core.Services;

namespace PlateGuard.Core.Tests.Services;

public sealed class PromotionUsageServiceTests
{
    [Fact]
    public async Task CheckEligibilityAsync_ReturnsEligibleForUnknownVehicleAndActivePromotion()
    {
        var service = CreateService(
            vehicleRepository: new FakeVehicleRepository(),
            promotionRepository: new FakePromotionRepository
            {
                PromotionById = new Promotion { Id = 3, PromotionName = "Weekend Promo", IsActive = true }
            });

        var result = await service.CheckEligibilityAsync("CSA-4653", 3);

        Assert.True(result.IsEligible);
        Assert.Equal("Vehicle is eligible for this promotion.", result.Message);
        Assert.NotNull(result.Promotion);
        Assert.Equal(3, result.Promotion!.Id);
        Assert.Null(result.Vehicle);
    }

    [Fact]
    public async Task CheckEligibilityAsync_ReturnsExistingUsageWhenPromotionAlreadyUsed()
    {
        var vehicle = new Vehicle
        {
            Id = 9,
            VehicleNumberRaw = "CSA-4653",
            VehicleNumberNormalized = "CSA4653",
            PhoneNumber = "0771234567"
        };
        var existingUsage = new PromotionUsage
        {
            Id = 15,
            VehicleId = 9,
            PromotionId = 3,
            ServiceDate = new DateTime(2026, 4, 9)
        };
        var service = CreateService(
            vehicleRepository: new FakeVehicleRepository { VehicleByNormalizedNumber = vehicle },
            promotionRepository: new FakePromotionRepository
            {
                PromotionById = new Promotion { Id = 3, PromotionName = "Weekend Promo", IsActive = true }
            },
            promotionUsageRepository: new FakePromotionUsageRepository
            {
                UsageByVehicleAndPromotion = existingUsage
            });

        var result = await service.CheckEligibilityAsync("CSA-4653", 3);

        Assert.False(result.IsEligible);
        Assert.Equal("Promotion already used for this vehicle.", result.Message);
        Assert.Same(vehicle, result.Vehicle);
        Assert.Same(existingUsage, result.ExistingUsage);
    }

    [Fact]
    public async Task SaveVehicleAndUsageAsync_ReturnsValidationFailureForNegativeAmountPaid()
    {
        var writer = new FakePromotionUsageTransactionalWriter();
        var service = CreateService(transactionalWriter: writer);

        var result = await service.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CSA-4653",
            PhoneNumber = "0771234567",
            PromotionId = 1,
            AmountPaid = -1
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Amount paid cannot be negative.", result.Message);
        Assert.False(writer.SaveCalled);
    }

    [Fact]
    public async Task SaveVehicleAndUsageAsync_NormalizesServiceDateAndMarksNewVehicle()
    {
        var writer = new FakePromotionUsageTransactionalWriter
        {
            SaveResult = new SavePromotionUsageResult
            {
                IsSuccess = true,
                Message = "Saved."
            }
        };
        var service = CreateService(
            promotionRepository: new FakePromotionRepository
            {
                PromotionById = new Promotion { Id = 5, PromotionName = "Weekend Promo", IsActive = true }
            },
            transactionalWriter: writer);

        var result = await service.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CSA-4653",
            PhoneNumber = "0771234567",
            PromotionId = 5,
            ServiceDate = new DateTime(2026, 4, 9, 18, 45, 12)
        });

        Assert.True(result.IsSuccess);
        Assert.True(result.CreatedNewVehicle);
        Assert.True(writer.SaveCalled);
        Assert.Null(writer.CapturedExistingVehicle);
        Assert.NotNull(writer.CapturedSaveRequest);
        Assert.Equal(new DateTime(2026, 4, 9), writer.CapturedSaveRequest!.ServiceDate);
    }

    [Fact]
    public async Task UpdateUsageRecordAsync_NormalizesServiceDateAndOptionalFieldsBeforeWriterCall()
    {
        var existingUsage = new PromotionUsage
        {
            Id = 40,
            VehicleId = 8,
            PromotionId = 2,
            ServiceDate = new DateTime(2026, 4, 1)
        };
        var vehicle = new Vehicle
        {
            Id = 8,
            VehicleNumberRaw = "ABC-1234",
            VehicleNumberNormalized = "ABC1234",
            PhoneNumber = "0700000000"
        };
        var writer = new FakePromotionUsageTransactionalWriter
        {
            UpdateResult = new OperationResult
            {
                IsSuccess = true,
                Message = "Record updated successfully."
            }
        };
        var service = CreateService(
            vehicleRepository: new FakeVehicleRepository { VehicleById = vehicle },
            promotionUsageRepository: new FakePromotionUsageRepository { UsageById = existingUsage },
            transactionalWriter: writer);

        var result = await service.UpdateUsageRecordAsync(new UpdatePromotionUsageRecordRequest
        {
            PromotionUsageId = 40,
            ServiceDate = new DateTime(2026, 4, 10, 13, 30, 0),
            PhoneNumber = " 0711111111 ",
            OwnerName = " Jane Doe ",
            Brand = " ",
            Model = " Prius ",
            Notes = "  note  "
        });

        Assert.True(result.IsSuccess);
        Assert.True(writer.UpdateCalled);
        Assert.NotNull(writer.CapturedUpdateVehicle);
        Assert.NotNull(writer.CapturedUpdateUsage);
        Assert.Equal(new DateTime(2026, 4, 10), writer.CapturedUpdateUsage!.ServiceDate);
        Assert.Equal("0711111111", writer.CapturedUpdateVehicle!.PhoneNumber);
        Assert.Equal("Jane Doe", writer.CapturedUpdateVehicle.OwnerName);
        Assert.Null(writer.CapturedUpdateVehicle.Brand);
        Assert.Equal("Prius", writer.CapturedUpdateVehicle.Model);
        Assert.Equal("note", writer.CapturedUpdateUsage.Notes);
    }

    [Fact]
    public async Task DeleteUsageAsync_RejectsIncorrectPassword()
    {
        var usageRepository = new FakePromotionUsageRepository();
        var service = CreateService(
            promotionUsageRepository: usageRepository,
            settingsRepository: new FakeSettingsRepository
            {
                Settings = new AppSettings
                {
                    Id = AppSettings.DefaultId,
                    DeletePasswordHash = DeletePasswordHasher.Hash("admin")
                }
            });

        var result = await service.DeleteUsageAsync(10, "wrong");

        Assert.False(result.IsSuccess);
        Assert.Equal("Incorrect delete password.", result.Message);
        Assert.Null(usageRepository.DeletedId);
    }

    [Fact]
    public async Task DeleteUsageAsync_DeletesWhenPasswordMatches()
    {
        var usageRepository = new FakePromotionUsageRepository();
        var service = CreateService(
            promotionUsageRepository: usageRepository,
            settingsRepository: new FakeSettingsRepository
            {
                Settings = new AppSettings
                {
                    Id = AppSettings.DefaultId,
                    DeletePasswordHash = DeletePasswordHasher.Hash("admin")
                }
            });

        var result = await service.DeleteUsageAsync(10, "admin");

        Assert.True(result.IsSuccess);
        Assert.Equal("Record deleted successfully.", result.Message);
        Assert.Equal(10, usageRepository.DeletedId);
    }

    [Fact]
    public async Task UpdateUsageAsync_BlocksDuplicateUsageWhenPairChanges()
    {
        var usageRepository = new FakePromotionUsageRepository
        {
            UsageById = new PromotionUsage
            {
                Id = 12,
                VehicleId = 1,
                PromotionId = 2,
                ServiceDate = new DateTime(2026, 4, 9)
            },
            ExistsResult = true
        };
        var service = CreateService(
            promotionRepository: new FakePromotionRepository
            {
                PromotionById = new Promotion { Id = 3, PromotionName = "Weekend Promo", IsActive = true }
            },
            promotionUsageRepository: usageRepository);

        var result = await service.UpdateUsageAsync(new PromotionUsage
        {
            Id = 12,
            VehicleId = 9,
            PromotionId = 3,
            ServiceDate = new DateTime(2026, 4, 10)
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("This vehicle has already used the selected promotion.", result.Message);
        Assert.Null(usageRepository.UpdatedUsage);
    }

    [Fact]
    public async Task UpdateUsageAsync_NormalizesServiceDateBeforeRepositoryUpdate()
    {
        var usageRepository = new FakePromotionUsageRepository
        {
            UsageById = new PromotionUsage
            {
                Id = 12,
                VehicleId = 1,
                PromotionId = 2,
                ServiceDate = new DateTime(2026, 4, 9)
            }
        };
        var service = CreateService(
            promotionRepository: new FakePromotionRepository
            {
                PromotionById = new Promotion { Id = 2, PromotionName = "Weekend Promo", IsActive = true }
            },
            promotionUsageRepository: usageRepository);

        var result = await service.UpdateUsageAsync(new PromotionUsage
        {
            Id = 12,
            VehicleId = 1,
            PromotionId = 2,
            ServiceDate = new DateTime(2026, 4, 10, 14, 5, 0)
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(usageRepository.UpdatedUsage);
        Assert.Equal(new DateTime(2026, 4, 10), usageRepository.UpdatedUsage!.ServiceDate);
    }

    private static PromotionUsageService CreateService(
        IVehicleRepository? vehicleRepository = null,
        IPromotionRepository? promotionRepository = null,
        IPromotionUsageRepository? promotionUsageRepository = null,
        ISettingsRepository? settingsRepository = null,
        IPromotionUsageTransactionalWriter? transactionalWriter = null)
    {
        return new PromotionUsageService(
            vehicleRepository ?? new FakeVehicleRepository(),
            promotionRepository ?? new FakePromotionRepository(),
            promotionUsageRepository ?? new FakePromotionUsageRepository(),
            settingsRepository ?? new FakeSettingsRepository(),
            transactionalWriter ?? new FakePromotionUsageTransactionalWriter());
    }

    private sealed class FakeVehicleRepository : IVehicleRepository
    {
        public Vehicle? VehicleById { get; set; }
        public Vehicle? VehicleByNormalizedNumber { get; set; }

        public Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(VehicleById);
        public Task<Vehicle?> GetByNormalizedNumberAsync(string vehicleNumber, CancellationToken cancellationToken = default) => Task.FromResult(VehicleByNormalizedNumber);
        public Task<IReadOnlyList<Vehicle>> SearchByPhoneNumberAsync(string phoneNumberQuery, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Vehicle>>([]);
        public Task<IReadOnlyList<Vehicle>> SearchByOwnerNameAsync(string ownerNameQuery, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Vehicle>>([]);
        public Task<Vehicle> AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default) => Task.FromResult(vehicle);
        public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakePromotionRepository : IPromotionRepository
    {
        public Promotion? PromotionById { get; set; }

        public Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(PromotionById);
        public Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Promotion>>([]);
        public Task<IReadOnlyList<Promotion>> GetActiveAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Promotion>>([]);
        public Task<Promotion> AddAsync(Promotion promotion, CancellationToken cancellationToken = default) => Task.FromResult(promotion);
        public Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakePromotionUsageRepository : IPromotionUsageRepository
    {
        public PromotionUsage? UsageById { get; set; }
        public PromotionUsage? UsageByVehicleAndPromotion { get; set; }
        public bool ExistsResult { get; set; }
        public int? DeletedId { get; private set; }
        public PromotionUsage? UpdatedUsage { get; private set; }

        public Task<PromotionUsage?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(UsageById);
        public Task<IReadOnlyList<PromotionUsage>> GetByVehicleIdAsync(int vehicleId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PromotionUsage>>([]);
        public Task<PromotionUsage?> GetByVehicleIdAndPromotionIdAsync(int vehicleId, int promotionId, CancellationToken cancellationToken = default) => Task.FromResult(UsageByVehicleAndPromotion);
        public Task<IReadOnlyList<PromotionUsageRecord>> SearchRecordsAsync(PromotionUsageRecordQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PromotionUsageRecord>>([]);
        public Task<int> CountByPromotionIdAsync(int promotionId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> ExistsAsync(int vehicleId, int promotionId, CancellationToken cancellationToken = default) => Task.FromResult(ExistsResult);
        public Task<PromotionUsage> AddAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default) => Task.FromResult(promotionUsage);

        public Task UpdateAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default)
        {
            UpdatedUsage = promotionUsage;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            DeletedId = id;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSettingsRepository : ISettingsRepository
    {
        public AppSettings? Settings { get; set; }

        public Task<AppSettings?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Settings);
        public Task<AppSettings> UpsertAsync(AppSettings settings, CancellationToken cancellationToken = default) => Task.FromResult(settings);
    }

    private sealed class FakePromotionUsageTransactionalWriter : IPromotionUsageTransactionalWriter
    {
        public bool SaveCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public Vehicle? CapturedExistingVehicle { get; private set; }
        public SavePromotionUsageRequest? CapturedSaveRequest { get; private set; }
        public Vehicle? CapturedUpdateVehicle { get; private set; }
        public PromotionUsage? CapturedUpdateUsage { get; private set; }
        public SavePromotionUsageResult SaveResult { get; set; } = new() { IsSuccess = true, Message = "Saved." };
        public OperationResult UpdateResult { get; set; } = new() { IsSuccess = true, Message = "Updated." };

        public Task<SavePromotionUsageResult> SaveVehicleAndUsageAsync(
            Vehicle? existingVehicle,
            SavePromotionUsageRequest request,
            CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            CapturedExistingVehicle = existingVehicle;
            CapturedSaveRequest = request;
            return Task.FromResult(SaveResult);
        }

        public Task<OperationResult> UpdateUsageRecordAsync(
            Vehicle vehicle,
            PromotionUsage promotionUsage,
            CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;
            CapturedUpdateVehicle = vehicle;
            CapturedUpdateUsage = promotionUsage;
            return Task.FromResult(UpdateResult);
        }
    }
}
