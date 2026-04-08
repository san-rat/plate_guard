using PlateGuard.Core.Helpers;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;

namespace PlateGuard.Core.Services;

public sealed class SettingsService(ISettingsRepository settingsRepository) : ISettingsService
{
    private readonly ISettingsRepository _settingsRepository = settingsRepository;

    public Task<AppSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        return GetOrCreateSettingsAsync(cancellationToken);
    }

    public async Task<UpdateAppSettingsResult> UpdateAsync(UpdateAppSettingsRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new UpdateAppSettingsRequest();

        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        var normalizedExportFolder = NormalizeOptionalText(request.ExportFolder);
        if (normalizedExportFolder is not null && !Path.IsPathFullyQualified(normalizedExportFolder))
        {
            return UpdateFailure("Export folder must be a full folder path.");
        }

        settings.ShopName = NormalizeOptionalText(request.ShopName);
        settings.ExportFolder = normalizedExportFolder;

        var savedSettings = await _settingsRepository.UpsertAsync(settings, cancellationToken);
        return UpdateSuccess(savedSettings, "Settings saved successfully.");
    }

    public async Task<OperationResult> ChangeDeletePasswordAsync(ChangeDeletePasswordRequest request, CancellationToken cancellationToken = default)
    {
        request ??= new ChangeDeletePasswordRequest();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return Failure("Current delete password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Failure("New delete password is required.");
        }

        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return Failure("New delete password confirmation does not match.");
        }

        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        if (!DeletePasswordHasher.Verify(request.CurrentPassword.Trim(), settings.DeletePasswordHash))
        {
            return Failure("Current delete password is incorrect.");
        }

        settings.DeletePasswordHash = DeletePasswordHasher.Hash(request.NewPassword.Trim());
        await _settingsRepository.UpsertAsync(settings, cancellationToken);

        return Success("Delete password changed successfully.");
    }

    private async Task<AppSettings> GetOrCreateSettingsAsync(CancellationToken cancellationToken)
    {
        var existingSettings = await _settingsRepository.GetAsync(cancellationToken);
        if (existingSettings is not null)
        {
            return existingSettings;
        }

        return await _settingsRepository.UpsertAsync(new AppSettings
        {
            Id = 1,
            DeletePasswordHash = DeletePasswordHasher.Hash("admin")
        }, cancellationToken);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static UpdateAppSettingsResult UpdateSuccess(AppSettings settings, string message)
    {
        return new UpdateAppSettingsResult
        {
            IsSuccess = true,
            Message = message,
            Settings = settings
        };
    }

    private static UpdateAppSettingsResult UpdateFailure(string message)
    {
        return new UpdateAppSettingsResult
        {
            IsSuccess = false,
            Message = message
        };
    }

    private static OperationResult Success(string message) => new() { IsSuccess = true, Message = message };
    private static OperationResult Failure(string message) => new() { IsSuccess = false, Message = message };
}
