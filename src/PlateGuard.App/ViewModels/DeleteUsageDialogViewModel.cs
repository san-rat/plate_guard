using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlateGuard.Core.Interfaces;

namespace PlateGuard.App.ViewModels;

public partial class DeleteUsageDialogViewModel : ViewModelBase
{
    private readonly IPromotionUsageService? _promotionUsageService;
    private readonly int _promotionUsageId;

    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    private string vehicleNumberDisplay = "-";

    [ObservableProperty]
    private string promotionName = "-";

    [ObservableProperty]
    private string warningMessage = "Deleting this record makes the vehicle eligible for this promotion again.";

    [ObservableProperty]
    private string deletePassword = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Enter the delete password to confirm.";

    [ObservableProperty]
    private bool isDeleting;

    public DeleteUsageDialogViewModel()
    {
        VehicleNumberDisplay = "CAB-1234";
        PromotionName = "Sample Promotion";
    }

    public DeleteUsageDialogViewModel(IPromotionUsageService promotionUsageService, DeleteUsageDialogRequest request)
    {
        _promotionUsageService = promotionUsageService;
        _promotionUsageId = request.Record.PromotionUsageId;
        VehicleNumberDisplay = string.IsNullOrWhiteSpace(request.Record.VehicleNumberRaw)
            ? request.Record.VehicleNumberNormalized
            : request.Record.VehicleNumberRaw;
        PromotionName = request.Record.PromotionName;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_promotionUsageService is null)
        {
            StatusMessage = "Delete is not available in design mode.";
            return;
        }

        if (string.IsNullOrWhiteSpace(DeletePassword))
        {
            StatusMessage = "Delete password is required.";
            return;
        }

        IsDeleting = true;
        try
        {
            var result = await _promotionUsageService.DeleteUsageAsync(_promotionUsageId, DeletePassword.Trim());
            StatusMessage = result.Message;
            if (result.IsSuccess)
            {
                CloseRequested?.Invoke(true);
            }
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not delete the record: {exception.Message}";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }
}
