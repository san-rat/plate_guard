using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Core.Services;

namespace PlateGuard.Core.Tests.Services;

public sealed class PromotionServiceTests
{
    [Fact]
    public async Task CreateAsync_SetsCreatedAtWhenMissing()
    {
        var repository = new CapturingPromotionRepository();
        var service = new PromotionService(repository);

        var createdPromotion = await service.CreateAsync(new Promotion
        {
            PromotionName = "New Year Promo"
        });

        Assert.NotNull(repository.AddedPromotion);
        Assert.Equal("New Year Promo", repository.AddedPromotion!.PromotionName);
        Assert.NotEqual(default, repository.AddedPromotion.CreatedAt);
        Assert.Same(repository.AddedPromotion, createdPromotion);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenPromotionNameIsMissing()
    {
        var repository = new CapturingPromotionRepository();
        var service = new PromotionService(repository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new Promotion
        {
            PromotionName = "   "
        }));

        Assert.Equal("promotion", exception.ParamName);
        Assert.Null(repository.AddedPromotion);
    }

    [Fact]
    public async Task ActivateAsync_SetsIsActiveAndPersistsUpdate()
    {
        var repository = new CapturingPromotionRepository
        {
            PromotionById = new Promotion
            {
                Id = 5,
                PromotionName = "April Promo",
                IsActive = false
            }
        };
        var service = new PromotionService(repository);

        var promotion = await service.ActivateAsync(5);

        Assert.True(promotion.IsActive);
        Assert.NotNull(promotion.UpdatedAt);
        Assert.NotNull(repository.UpdatedPromotion);
        Assert.True(repository.UpdatedPromotion!.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveFalseAndPersistsUpdate()
    {
        var repository = new CapturingPromotionRepository
        {
            PromotionById = new Promotion
            {
                Id = 8,
                PromotionName = "April Promo",
                IsActive = true
            }
        };
        var service = new PromotionService(repository);

        var promotion = await service.DeactivateAsync(8);

        Assert.False(promotion.IsActive);
        Assert.NotNull(repository.UpdatedPromotion);
        Assert.False(repository.UpdatedPromotion!.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var repository = new CapturingPromotionRepository();
        var service = new PromotionService(repository);
        var promotion = new Promotion
        {
            Id = 10,
            PromotionName = "Weekend Promo",
            IsActive = true
        };

        var updated = await service.UpdateAsync(promotion);

        Assert.Same(promotion, updated);
        Assert.NotNull(updated.UpdatedAt);
        Assert.Same(promotion, repository.UpdatedPromotion);
    }

    [Fact]
    public async Task ActivateAsync_ThrowsWhenPromotionIsMissing()
    {
        var repository = new CapturingPromotionRepository();
        var service = new PromotionService(repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ActivateAsync(99));

        Assert.Equal("Promotion with id 99 was not found.", exception.Message);
    }

    private sealed class CapturingPromotionRepository : IPromotionRepository
    {
        public Promotion? PromotionById { get; set; }
        public Promotion? AddedPromotion { get; private set; }
        public Promotion? UpdatedPromotion { get; private set; }

        public Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PromotionById);
        }

        public Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Promotion>>([]);
        }

        public Task<IReadOnlyList<Promotion>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Promotion>>([]);
        }

        public Task<Promotion> AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
        {
            AddedPromotion = promotion;
            return Task.FromResult(promotion);
        }

        public Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default)
        {
            UpdatedPromotion = promotion;
            return Task.CompletedTask;
        }
    }
}
