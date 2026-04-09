using PlateGuard.Core.Helpers;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Core.Services;

namespace PlateGuard.Core.Tests.Services;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task GetAsync_CreatesDefaultSettingsWhenMissing()
    {
        var repository = new InMemorySettingsRepository();
        var service = new SettingsService(repository);

        var settings = await service.GetAsync();

        Assert.Equal(AppSettings.DefaultId, settings.Id);
        Assert.True(DeletePasswordHasher.Verify("admin", settings.DeletePasswordHash));
        Assert.Equal(1, repository.UpsertCallCount);
    }

    [Fact]
    public async Task GetAsync_ReturnsExistingSettingsWithoutCreatingNewRow()
    {
        var repository = new InMemorySettingsRepository(new AppSettings
        {
            Id = AppSettings.DefaultId,
            DeletePasswordHash = DeletePasswordHasher.Hash("admin"),
            ShopName = "Existing Shop"
        });
        var service = new SettingsService(repository);

        var settings = await service.GetAsync();

        Assert.Equal("Existing Shop", settings.ShopName);
        Assert.Equal(0, repository.UpsertCallCount);
    }

    [Fact]
    public async Task UpdateAsync_RejectsRelativeExportFolder()
    {
        var repository = new InMemorySettingsRepository(new AppSettings
        {
            Id = AppSettings.DefaultId,
            DeletePasswordHash = DeletePasswordHasher.Hash("admin")
        });
        var service = new SettingsService(repository);

        var result = await service.UpdateAsync(new UpdateAppSettingsRequest
        {
            ShopName = " Test Shop ",
            ExportFolder = "relative\\path"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Export folder must be a full folder path.", result.Message);
        Assert.Equal(0, repository.UpsertCallCount);
        Assert.Null(repository.CurrentSettings!.ShopName);
        Assert.Null(repository.CurrentSettings.ExportFolder);
    }

    [Fact]
    public async Task UpdateAsync_TrimsAndPersistsShopNameAndExportFolder()
    {
        var repository = new InMemorySettingsRepository(new AppSettings
        {
            Id = AppSettings.DefaultId,
            DeletePasswordHash = DeletePasswordHasher.Hash("admin")
        });
        var service = new SettingsService(repository);
        var exportFolder = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "PlateGuard.Settings.Tests"));

        var result = await service.UpdateAsync(new UpdateAppSettingsRequest
        {
            ShopName = " Test Shop ",
            ExportFolder = $"  {exportFolder}  "
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Test Shop", repository.CurrentSettings!.ShopName);
        Assert.Equal(exportFolder, repository.CurrentSettings.ExportFolder);
        Assert.Equal(1, repository.UpsertCallCount);
    }

    [Fact]
    public async Task ChangeDeletePasswordAsync_UpdatesHashWhenCurrentPasswordMatches()
    {
        var repository = new InMemorySettingsRepository(new AppSettings
        {
            Id = AppSettings.DefaultId,
            DeletePasswordHash = DeletePasswordHasher.Hash("admin")
        });
        var service = new SettingsService(repository);

        var result = await service.ChangeDeletePasswordAsync(new ChangeDeletePasswordRequest
        {
            CurrentPassword = " admin ",
            NewPassword = "new-secret",
            ConfirmNewPassword = "new-secret"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.UpsertCallCount);
        Assert.True(DeletePasswordHasher.Verify("new-secret", repository.CurrentSettings!.DeletePasswordHash));
    }

    [Fact]
    public async Task ChangeDeletePasswordAsync_RejectsWhenConfirmationDoesNotMatch()
    {
        var repository = new InMemorySettingsRepository(new AppSettings
        {
            Id = AppSettings.DefaultId,
            DeletePasswordHash = DeletePasswordHasher.Hash("admin")
        });
        var service = new SettingsService(repository);

        var result = await service.ChangeDeletePasswordAsync(new ChangeDeletePasswordRequest
        {
            CurrentPassword = "admin",
            NewPassword = "new-secret",
            ConfirmNewPassword = "different"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("New delete password confirmation does not match.", result.Message);
        Assert.Equal(0, repository.UpsertCallCount);
    }

    [Fact]
    public async Task ChangeDeletePasswordAsync_RejectsIncorrectCurrentPassword()
    {
        var repository = new InMemorySettingsRepository(new AppSettings
        {
            Id = AppSettings.DefaultId,
            DeletePasswordHash = DeletePasswordHasher.Hash("admin")
        });
        var originalHash = repository.CurrentSettings!.DeletePasswordHash;
        var service = new SettingsService(repository);

        var result = await service.ChangeDeletePasswordAsync(new ChangeDeletePasswordRequest
        {
            CurrentPassword = "wrong",
            NewPassword = "new-secret",
            ConfirmNewPassword = "new-secret"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("Current delete password is incorrect.", result.Message);
        Assert.Equal(0, repository.UpsertCallCount);
        Assert.Equal(originalHash, repository.CurrentSettings.DeletePasswordHash);
    }

    private sealed class InMemorySettingsRepository : ISettingsRepository
    {
        public InMemorySettingsRepository(AppSettings? currentSettings = null)
        {
            CurrentSettings = currentSettings;
        }

        public AppSettings? CurrentSettings { get; private set; }
        public int UpsertCallCount { get; private set; }

        public Task<AppSettings?> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CurrentSettings);
        }

        public Task<AppSettings> UpsertAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            UpsertCallCount++;
            CurrentSettings = settings;
            return Task.FromResult(settings);
        }
    }
}
