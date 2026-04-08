using PlateGuard.Core.Models;

namespace PlateGuard.Core.Interfaces;

public interface ISettingsRepository
{
    Task<AppSettings?> GetAsync(CancellationToken cancellationToken = default);
    Task<AppSettings> UpsertAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
