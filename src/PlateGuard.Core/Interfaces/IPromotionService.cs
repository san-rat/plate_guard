using PlateGuard.Core.Models;

namespace PlateGuard.Core.Interfaces;

public interface IPromotionService
{
    Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Promotion> CreateAsync(Promotion promotion, CancellationToken cancellationToken = default);
    Task<Promotion> UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default);
    Task<Promotion> ActivateAsync(int id, CancellationToken cancellationToken = default);
    Task<Promotion> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
