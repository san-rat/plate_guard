using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;

namespace PlateGuard.App.ViewModels;

public partial class EditUsageDialogViewModel : ViewModelBase
{
    private readonly IPromotionUsageService? _promotionUsageService;
    private readonly int _promotionUsageId;

    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    private string vehicleNumberDisplay = "-";

    [ObservableProperty]
    private string promotionName = "-";

    [ObservableProperty]
    private string serviceDateText = string.Empty;

    [ObservableProperty]
    private string phoneNumber = string.Empty;

    [ObservableProperty]
    private string ownerName = string.Empty;

    [ObservableProperty]
    private string brand = string.Empty;

    [ObservableProperty]
    private string model = string.Empty;

    [ObservableProperty]
    private string mileageText = string.Empty;

    [ObservableProperty]
    private string normalPriceText = string.Empty;

    [ObservableProperty]
    private string discountedPriceText = string.Empty;

    [ObservableProperty]
    private string amountPaidText = string.Empty;

    [ObservableProperty]
    private string notes = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Phone number is required. Dates use yyyy-MM-dd.";

    [ObservableProperty]
    private bool isSaving;

    public EditUsageDialogViewModel()
    {
        VehicleNumberDisplay = "CAB-1234";
        PromotionName = "Sample Promotion";
        ServiceDateText = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        PhoneNumber = "0771234567";
        OwnerName = "Nimal Perera";
        Brand = "Toyota";
        Model = "Corolla";
        MileageText = "45000";
        NormalPriceText = "7500";
        DiscountedPriceText = "5000";
        AmountPaidText = "5000";
        Notes = "Design preview";
    }

    public EditUsageDialogViewModel(IPromotionUsageService promotionUsageService, EditUsageDialogRequest request)
    {
        _promotionUsageService = promotionUsageService;
        _promotionUsageId = request.Record.PromotionUsageId;
        VehicleNumberDisplay = string.IsNullOrWhiteSpace(request.Record.VehicleNumberRaw)
            ? request.Record.VehicleNumberNormalized
            : request.Record.VehicleNumberRaw;
        PromotionName = request.Record.PromotionName;
        ServiceDateText = request.Record.ServiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        PhoneNumber = request.Record.PhoneNumber;
        OwnerName = request.Record.OwnerName ?? string.Empty;
        Brand = request.Record.Brand ?? string.Empty;
        Model = request.Record.Model ?? string.Empty;
        MileageText = FormatNullableInt(request.Record.Mileage);
        NormalPriceText = FormatNullableDecimal(request.Record.NormalPrice);
        DiscountedPriceText = FormatNullableDecimal(request.Record.DiscountedPrice);
        AmountPaidText = FormatNullableDecimal(request.Record.AmountPaid);
        Notes = request.Record.Notes ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_promotionUsageService is null)
        {
            StatusMessage = "Save is not available in design mode.";
            return;
        }

        var validation = Validate(out var serviceDate);
        if (validation is not null)
        {
            StatusMessage = validation;
            return;
        }

        IsSaving = true;
        try
        {
            var request = new UpdatePromotionUsageRecordRequest
            {
                PromotionUsageId = _promotionUsageId,
                ServiceDate = serviceDate,
                PhoneNumber = PhoneNumber.Trim(),
                OwnerName = NormalizeOptionalText(OwnerName),
                Brand = NormalizeOptionalText(Brand),
                Model = NormalizeOptionalText(Model),
                Mileage = ParseNullableInt(MileageText),
                NormalPrice = ParseNullableDecimal(NormalPriceText),
                DiscountedPrice = ParseNullableDecimal(DiscountedPriceText),
                AmountPaid = ParseNullableDecimal(AmountPaidText),
                Notes = NormalizeOptionalText(Notes)
            };

            var result = await _promotionUsageService.UpdateUsageRecordAsync(request);
            StatusMessage = result.Message;
            if (result.IsSuccess)
            {
                CloseRequested?.Invoke(true);
            }
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not update the record: {exception.Message}";
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

    private string? Validate(out DateTime serviceDate)
    {
        serviceDate = default;

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            return "Phone number is required.";
        }

        if (!TryParseRequiredDate(ServiceDateText, out serviceDate))
        {
            return "Service date must be a valid date in yyyy-MM-dd format.";
        }

        if (!TryParseNullableInt(MileageText, out var mileage))
        {
            return "Mileage must be a whole number.";
        }

        if (mileage < 0)
        {
            return "Mileage cannot be negative.";
        }

        if (!TryParseNullableDecimal(NormalPriceText, out var normalPrice))
        {
            return "Normal price must be numeric.";
        }

        if (normalPrice < 0)
        {
            return "Normal price cannot be negative.";
        }

        if (!TryParseNullableDecimal(DiscountedPriceText, out var discountedPrice))
        {
            return "Discounted price must be numeric.";
        }

        if (discountedPrice < 0)
        {
            return "Discounted price cannot be negative.";
        }

        if (!TryParseNullableDecimal(AmountPaidText, out var amountPaid))
        {
            return "Amount paid must be numeric.";
        }

        if (amountPaid < 0)
        {
            return "Amount paid cannot be negative.";
        }

        return null;
    }

    private static bool TryParseRequiredDate(string value, out DateTime serviceDate)
    {
        return DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out serviceDate)
               || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out serviceDate);
    }

    private static string? NormalizeOptionalText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FormatNullableInt(int? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }

    private static string FormatNullableDecimal(decimal? value)
    {
        return value.HasValue ? value.Value.ToString("0.00", CultureInfo.InvariantCulture) : string.Empty;
    }

    private static int? ParseNullableInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsedValue)
            ? parsedValue
            : null;
    }

    private static bool TryParseNullableInt(string value, out int? parsedValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsedValue = null;
            return true;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var integerValue))
        {
            parsedValue = integerValue;
            return true;
        }

        parsedValue = null;
        return false;
    }

    private static decimal? ParseNullableDecimal(string value)
    {
        return TryParseNullableDecimal(value, out var parsedValue) ? parsedValue : null;
    }

    private static bool TryParseNullableDecimal(string value, out decimal? parsedValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsedValue = null;
            return true;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentCultureValue) ||
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out currentCultureValue))
        {
            parsedValue = currentCultureValue;
            return true;
        }

        parsedValue = null;
        return false;
    }
}
