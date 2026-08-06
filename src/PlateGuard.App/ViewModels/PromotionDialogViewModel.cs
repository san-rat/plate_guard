using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;

namespace PlateGuard.App.ViewModels;

public partial class PromotionDialogViewModel : ViewModelBase
{
    private readonly IPromotionService? _promotionService;
    private readonly int _promotionId;

    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    private string dialogTitle = "Add Promotion";

    [ObservableProperty]
    private string saveButtonText = "Save";

    [ObservableProperty]
    private string promotionName = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? startDate;

    [ObservableProperty]
    private DateTimeOffset? endDate;

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private string statusMessage = "Promotion name is required. Dates are optional.";

    [ObservableProperty]
    private bool isSaving;

    public Promotion? LastSavedPromotion { get; private set; }

    public PromotionDialogViewModel()
    {
        DialogTitle = "Edit Promotion";
        SaveButtonText = "Save Changes";
        PromotionName = "Sample Promotion";
        Description = "New Year special";
        StartDate = new DateTimeOffset(DateTime.Today);
        EndDate = new DateTimeOffset(DateTime.Today.AddDays(30));
        IsActive = true;
    }

    public PromotionDialogViewModel(IPromotionService promotionService, PromotionDialogRequest request)
    {
        _promotionService = promotionService;

        if (request.Promotion is null)
        {
            DialogTitle = "Add Promotion";
            SaveButtonText = "Save";
            IsActive = true;
            return;
        }

        _promotionId = request.Promotion.Id;
        DialogTitle = "Edit Promotion";
        SaveButtonText = "Save Changes";
        PromotionName = request.Promotion.PromotionName;
        Description = request.Promotion.Description ?? string.Empty;
        StartDate = ToOffset(request.Promotion.StartDate);
        EndDate = ToOffset(request.Promotion.EndDate);
        IsActive = request.Promotion.IsActive;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_promotionService is null)
        {
            StatusMessage = "Save is not available in design mode.";
            return;
        }

        var validation = Validate(out var startDate, out var endDate);
        if (validation is not null)
        {
            StatusMessage = validation;
            return;
        }

        IsSaving = true;
        try
        {
            var promotion = new Promotion
            {
                Id = _promotionId,
                PromotionName = PromotionName.Trim(),
                Description = NormalizeOptionalText(Description),
                StartDate = startDate,
                EndDate = endDate,
                IsActive = IsActive
            };

            LastSavedPromotion = _promotionId == 0
                ? await _promotionService.CreateAsync(promotion)
                : await _promotionService.UpdateAsync(promotion);

            StatusMessage = "Promotion saved successfully.";
            CloseRequested?.Invoke(true);
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not save the promotion: {exception.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }

    private string? Validate(out DateTime? startDate, out DateTime? endDate)
    {
        startDate = null;
        endDate = null;

        if (string.IsNullOrWhiteSpace(PromotionName))
        {
            return "Promotion name is required.";
        }

        // The picker cannot produce an unparseable value, so only the ordering needs checking.
        startDate = StartDate?.Date;
        endDate = EndDate?.Date;

        if (startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date)
        {
            return "End date cannot be earlier than start date.";
        }

        return null;
    }

    private static string? NormalizeOptionalText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTimeOffset? ToOffset(DateTime? value)
    {
        return value.HasValue ? new DateTimeOffset(value.Value.Date) : null;
    }
}
