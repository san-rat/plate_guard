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
    private string startDateText = string.Empty;

    [ObservableProperty]
    private string endDateText = string.Empty;

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private string statusMessage = "Promotion name is required. Dates use yyyy-MM-dd if entered.";

    [ObservableProperty]
    private bool isSaving;

    public Promotion? LastSavedPromotion { get; private set; }

    public PromotionDialogViewModel()
    {
        DialogTitle = "Edit Promotion";
        SaveButtonText = "Save Changes";
        PromotionName = "Sample Promotion";
        Description = "New Year special";
        StartDateText = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        EndDateText = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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
        StartDateText = FormatDate(request.Promotion.StartDate);
        EndDateText = FormatDate(request.Promotion.EndDate);
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

        if (!TryParseOptionalDate(StartDateText, out startDate))
        {
            return "Start date must be a valid date in yyyy-MM-dd format.";
        }

        if (!TryParseOptionalDate(EndDateText, out endDate))
        {
            return "End date must be a valid date in yyyy-MM-dd format.";
        }

        if (startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date)
        {
            return "End date cannot be earlier than start date.";
        }

        return null;
    }

    private static bool TryParseOptionalDate(string value, out DateTime? parsedDate)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsedDate = null;
            return true;
        }

        if (DateTime.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var exactDate) ||
            DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out exactDate))
        {
            parsedDate = exactDate.Date;
            return true;
        }

        parsedDate = null;
        return false;
    }

    private static string? NormalizeOptionalText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
    }
}
