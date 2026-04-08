using PlateGuard.Core.Models;

namespace PlateGuard.Core.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<UpdateAppSettingsResult> UpdateAsync(UpdateAppSettingsRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> ChangeDeletePasswordAsync(ChangeDeletePasswordRequest request, CancellationToken cancellationToken = default);
}
