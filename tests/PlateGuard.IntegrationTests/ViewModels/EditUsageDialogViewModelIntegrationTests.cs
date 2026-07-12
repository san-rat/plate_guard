using PlateGuard.App.ViewModels;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;
using PlateGuard.IntegrationTests.Infrastructure;

namespace PlateGuard.IntegrationTests.ViewModels;

public sealed class EditUsageDialogViewModelIntegrationTests
{
    [Fact]
    public async Task ServiceDate_ChosenDatePersistsAsDateOnly()
    {
        await using var app = await IntegrationTestApp.CreateAsync();

        var promotionService = app.GetRequiredService<IPromotionService>();
        var promotionUsageService = app.GetRequiredService<IPromotionUsageService>();

        var promotion = await promotionService.CreateAsync(new Promotion
        {
            PromotionName = "Edit Date Promo",
            IsActive = true
        });

        var saveResult = await promotionUsageService.SaveVehicleAndUsageAsync(new SavePromotionUsageRequest
        {
            VehicleNumberRaw = "CAB-4001",
            PhoneNumber = "0774004001",
            OwnerName = "Edit Date Owner",
            PromotionId = promotion.Id,
            ServiceDate = new DateTime(2026, 5, 1, 9, 30, 0)
        });

        Assert.True(saveResult.IsSuccess);

        var record = Assert.Single(await promotionUsageService.SearchUsageRecordsAsync(new PromotionUsageRecordQuery()));
        var viewModel = new EditUsageDialogViewModel(
            promotionUsageService,
            new EditUsageDialogRequest
            {
                Record = record
            });

        viewModel.ServiceDate = new DateTimeOffset(2026, 5, 14, 18, 45, 0, TimeSpan.Zero);

        await viewModel.SaveCommand.ExecuteAsync(null);

        var updatedRecord = Assert.Single(await promotionUsageService.SearchUsageRecordsAsync(new PromotionUsageRecordQuery()));
        Assert.Equal(new DateTime(2026, 5, 14), updatedRecord.ServiceDate);
    }
}
