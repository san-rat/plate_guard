using PlateGuard.Core.Models;

namespace PlateGuard.Core.Interfaces;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Promotion>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Promotion> AddAsync(Promotion promotion, CancellationToken cancellationToken = default);
    Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default);
}
