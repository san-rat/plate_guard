using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.IntegrationTests.Infrastructure;

namespace PlateGuard.IntegrationTests.ServiceFlows;

public sealed class SettingsExportAndDeleteIntegrationTests
{
    [Fact]
    public async Task SettingsExportAndDeletePasswordFlow_WorksEndToEnd()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var root = app.RootDirectory;
        var exportFolder = Path.Combine(root, "exports");

        var settingsService = app.GetRequiredService<ISettingsService>();
        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();
        var exportService = app.GetRequiredService<IExportService>();

        var settingsUpdate = await settingsService.UpdateAsync(new UpdateAppSettingsRequest
        {
            ShopName = "Integration Shop",
            ExportFolder = exportFolder
        });
        Assert.True(settingsUpdate.IsSuccess);

        var promotion = await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "Export Promo",
            IsActive = true
        });

        var saveResult = await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "ABC-9090",
            PhoneNumber = "0779999999",
            OwnerName = "Jane \"Owner\"",
            PromotionId = promotion.Id,
            Notes = "Line 1\r\nLine 2",
            AmountPaid = 1250m
        });
        Assert.True(saveResult.IsSuccess);

        var records = await promotionUsageService.SearchUsageRecordsAsync(new PromotionUsageRecordQuery());
        Assert.Single(records);

        var exportResult = await exportService.ExportPromotionUsageRecordsAsync(new ExportPromotionUsageRecordsRequest
        {
            Records = records
        });
        Assert.True(exportResult.IsSuccess);
        Assert.NotNull(exportResult.FilePath);
        Assert.True(File.Exists(exportResult.FilePath));

        var exportContent = await File.ReadAllTextAsync(exportResult.FilePath!);
        Assert.Contains("Export Promo", exportContent, StringComparison.Ordinal);
        Assert.Contains("\"Jane \"\"Owner\"\"\"", exportContent, StringComparison.Ordinal);

        var passwordChange = await settingsService.ChangeDeletePasswordAsync(new ChangeDeletePasswordRequest
        {
            CurrentPassword = "admin",
            NewPassword = "integration-secret",
            ConfirmNewPassword = "integration-secret"
        });
        Assert.True(passwordChange.IsSuccess);

        var deleteWithOldPassword = await promotionUsageService.DeleteUsageAsync(saveResult.PromotionUsage!.Id, "admin");
        Assert.False(deleteWithOldPassword.IsSuccess);

        var deleteWithNewPassword = await promotionUsageService.DeleteUsageAsync(saveResult.PromotionUsage.Id, "integration-secret");
        Assert.True(deleteWithNewPassword.IsSuccess);

        var remainingRecords = await promotionUsageService.SearchUsageRecordsAsync(new PromotionUsageRecordQuery());
        Assert.Empty(remainingRecords);
    }
}
