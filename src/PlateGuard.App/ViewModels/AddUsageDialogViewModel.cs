using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlateGuard.Core.Helpers;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;

namespace PlateGuard.App.ViewModels;

public partial class AddUsageDialogViewModel : ViewModelBase
{
    private readonly IPromotionUsageService? _promotionUsageService;

    public ObservableCollection<Promotion> AvailablePromotions { get; } = [];

    public event Action<bool>? CloseRequested;

    [ObservableProperty]
    private string entryModeTitle = "Add Usage";

    [ObservableProperty]
    private string entryModeMessage = "Required fields are marked clearly. Save will reuse an existing vehicle when possible.";

    [ObservableProperty]
    private string vehicleNumberRaw = string.Empty;

    [ObservableProperty]
    private string normalizedVehicleNumber = "-";

    [ObservableProperty]
    private Promotion? selectedPromotion;

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
    private string statusMessage = "Required fields are marked with *.";

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private bool isExistingVehicle;

    public SavePromotionUsageResult? LastSaveResult { get; private set; }

    public bool CanEditVehicleNumber => !IsExistingVehicle;

    public AddUsageDialogViewModel()
    {
        AvailablePromotions.Add(new Promotion { Id = 1, PromotionName = "Sample Promotion", IsActive = true });
        SelectedPromotion = AvailablePromotions[0];
        VehicleNumberRaw = "CAB-1234";
        PhoneNumber = "0771234567";
        OwnerName = "Nimal Perera";
        Brand = "Toyota";
        Model = "Corolla";
        MileageText = "45000";
        NormalPriceText = "7500";
        DiscountedPriceText = "5000";
        AmountPaidText = "5000";
        Notes = "Design preview";
        EntryModeTitle = "Existing vehicle";
        EntryModeMessage = "Vehicle details were found from search and can be completed before saving.";
        IsExistingVehicle = true;
        UpdateNormalizedVehicleNumber(VehicleNumberRaw);
    }

    public AddUsageDialogViewModel(
        IPromotionUsageService promotionUsageService,
        AddUsageDialogRequest request)
    {
        _promotionUsageService = promotionUsageService;

        foreach (var promotion in request.AvailablePromotions.OrderBy(promotion => promotion.PromotionName))
        {
            AvailablePromotions.Add(promotion);
        }

        SelectedPromotion = request.SelectedPromotion is null
            ? AvailablePromotions.FirstOrDefault()
            : AvailablePromotions.FirstOrDefault(promotion => promotion.Id == request.SelectedPromotion.Id)
              ?? request.SelectedPromotion;

        if (request.Vehicle is not null)
        {
            IsExistingVehicle = true;
            EntryModeTitle = "Existing vehicle";
            EntryModeMessage = "Vehicle details were found from search. You can adjust non-key details before saving.";
            VehicleNumberRaw = request.Vehicle.VehicleNumberRaw;
            PhoneNumber = request.Vehicle.PhoneNumber;
            OwnerName = request.Vehicle.OwnerName ?? string.Empty;
            Brand = request.Vehicle.Brand ?? string.Empty;
            Model = request.Vehicle.Model ?? string.Empty;
        }
        else
        {
            IsExistingVehicle = false;
            EntryModeTitle = "New vehicle";
            EntryModeMessage = "No existing vehicle matched. Enter the required fields and save the new usage.";
            VehicleNumberRaw = request.PrefilledVehicleNumber;
        }

        UpdateNormalizedVehicleNumber(VehicleNumberRaw);
    }

    partial void OnVehicleNumberRawChanged(string value)
    {
        UpdateNormalizedVehicleNumber(value);
    }

    partial void OnIsExistingVehicleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditVehicleNumber));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_promotionUsageService is null)
        {
            StatusMessage = "Save is not available in design mode.";
            return;
        }

        var validation = Validate();
        if (validation is not null)
        {
            StatusMessage = validation;
            return;
        }

        IsSaving = true;
        try
        {
            var saveRequest = new SavePromotionUsageRequest
            {
                VehicleNumberRaw = VehicleNumberRaw.Trim(),
                PhoneNumber = PhoneNumber.Trim(),
                OwnerName = NormalizeOptionalText(OwnerName),
                Brand = NormalizeOptionalText(Brand),
                Model = NormalizeOptionalText(Model),
                PromotionId = SelectedPromotion!.Id,
                Mileage = ParseNullableInt(MileageText),
                NormalPrice = ParseNullableDecimal(NormalPriceText),
                DiscountedPrice = ParseNullableDecimal(DiscountedPriceText),
                AmountPaid = ParseNullableDecimal(AmountPaidText),
                Notes = NormalizeOptionalText(Notes),
                ServiceDate = DateTime.Today
            };

            LastSaveResult = await _promotionUsageService.SaveVehicleAndUsageAsync(saveRequest);
            StatusMessage = LastSaveResult.Message;

            if (LastSaveResult.IsSuccess)
            {
                CloseRequested?.Invoke(true);
            }
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not save the usage record: {exception.Message}";
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

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(VehicleNumberRaw))
        {
            return "Vehicle number is required.";
        }

        if (string.IsNullOrWhiteSpace(NormalizedVehicleNumber) || NormalizedVehicleNumber == "-")
        {
            return "Enter a valid vehicle number.";
        }

        if (SelectedPromotion is null)
        {
            return "Promotion is required.";
        }

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            return "Phone number is required.";
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

    private void UpdateNormalizedVehicleNumber(string value)
    {
        NormalizedVehicleNumber = string.IsNullOrWhiteSpace(value)
            ? "-"
            : VehicleNumberNormalizer.Normalize(value);

        if (string.IsNullOrWhiteSpace(NormalizedVehicleNumber))
        {
            NormalizedVehicleNumber = "-";
        }
    }

    private static string? NormalizeOptionalText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
