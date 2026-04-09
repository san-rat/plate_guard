using System.Text;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.Core.Services;

namespace PlateGuard.Core.Tests.Services;

public sealed class ExportServiceTests
{
    [Fact]
    public async Task ExportPromotionUsageRecordsAsync_ReturnsFailureWhenNoRecords()
    {
        var service = new ExportService(new FakeSettingsRepository());

        var result = await service.ExportPromotionUsageRecordsAsync(new ExportPromotionUsageRecordsRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("No records are available to export.", result.Message);
    }

    [Fact]
    public async Task ExportPromotionUsageRecordsAsync_UsesSettingsFolderAndWritesCsv()
    {
        var root = CreateTempDirectory();
        try
        {
            var exportFolder = Path.Combine(root, "exports");
            var service = new ExportService(new FakeSettingsRepository
            {
                Settings = new AppSettings
                {
                    Id = AppSettings.DefaultId,
                    ExportFolder = exportFolder
                }
            });

            var result = await service.ExportPromotionUsageRecordsAsync(new ExportPromotionUsageRecordsRequest
            {
                Records =
                [
                    new PromotionUsageRecord
                    {
                        ServiceDate = new DateTime(2026, 4, 9),
                        PromotionName = "Weekend Promo",
                        VehicleNumberRaw = "CSA-4653",
                        PhoneNumber = "0771234567",
                        OwnerName = "Jane, \"J\"",
                        Notes = "Line 1\r\nLine 2",
                        AmountPaid = 1250m
                    }
                ]
            });

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.FilePath);
            Assert.StartsWith(exportFolder, result.FilePath!, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(result.FilePath));

            var content = await File.ReadAllTextAsync(result.FilePath!, Encoding.UTF8);
            Assert.Contains("Service Date,Promotion Name,Vehicle Number", content);
            Assert.Contains("\"Jane, \"\"J\"\"\"", content);
            Assert.Contains("\"Line 1", content);
            Assert.Contains("1250.00", content);
        }
        finally
        {
            DeleteDirectoryIfExists(root);
        }
    }

    [Fact]
    public async Task ExportPromotionUsageRecordsAsync_ReturnsFailureWhenExportFolderIsExistingFile()
    {
        var root = CreateTempDirectory();
        try
        {
            var exportFilePath = Path.Combine(root, "not-a-folder.txt");
            await File.WriteAllTextAsync(exportFilePath, "blocked");
            var service = new ExportService(new FakeSettingsRepository());

            var result = await service.ExportPromotionUsageRecordsAsync(new ExportPromotionUsageRecordsRequest
            {
                ExportFolder = exportFilePath,
                Records =
                [
                    new PromotionUsageRecord
                    {
                        ServiceDate = new DateTime(2026, 4, 9),
                        PromotionName = "Weekend Promo",
                        VehicleNumberRaw = "CSA-4653",
                        PhoneNumber = "0771234567"
                    }
                ]
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("Export folder must point to a folder, not an existing file.", result.Message);
        }
        finally
        {
            DeleteDirectoryIfExists(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "PlateGuard.Core.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class FakeSettingsRepository : ISettingsRepository
    {
        public AppSettings? Settings { get; set; }

        public Task<AppSettings?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Settings);
        public Task<AppSettings> UpsertAsync(AppSettings settings, CancellationToken cancellationToken = default) => Task.FromResult(settings);
    }
}
