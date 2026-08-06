using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
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
    private readonly IVehicleService? _vehicleService;
    private CancellationTokenSource? _ownerSuggestionDebounceCts;
    private CancellationTokenSource? _eligibilityDebounceCts;
    private bool _suppressOwnerSuggestionSearch;

    public ObservableCollection<Promotion> AvailablePromotions { get; } = [];
    public ObservableCollection<OwnerSuggestion> OwnerSuggestions { get; } = [];
    public ObservableCollection<Vehicle> ReusableVehicles { get; } = [];

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
    private DateTimeOffset? serviceDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string eligibilityHint = "Choose a promotion and enter a vehicle number to preview eligibility.";

    [ObservableProperty]
    private EligibilityHintTone eligibilityHintTone;

    [ObservableProperty]
    private bool isEligibilityHintPositive;

    [ObservableProperty]
    private bool isEligibilityHintNegative;

    [ObservableProperty]
    private bool isEligibilityHintWarning;

    [ObservableProperty]
    private string phoneNumber = string.Empty;

    [ObservableProperty]
    private string ownerName = string.Empty;

    [ObservableProperty]
    private bool isOwnerSuggestionsOpen;

    [ObservableProperty]
    private bool hasReusableVehicles;

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
        IVehicleService? vehicleService,
        AddUsageDialogRequest request)
    {
        _promotionUsageService = promotionUsageService;
        _vehicleService = vehicleService;

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
            PhoneNumber = request.PrefilledPhoneNumber;
            OwnerName = request.PrefilledOwnerName;
        }

        UpdateNormalizedVehicleNumber(VehicleNumberRaw);
    }

    partial void OnVehicleNumberRawChanged(string value)
    {
        UpdateNormalizedVehicleNumber(value);
    }

    partial void OnNormalizedVehicleNumberChanged(string value)
    {
        _ = DebouncedUpdateEligibilityHintAsync();
    }

    partial void OnSelectedPromotionChanged(Promotion? value)
    {
        _ = DebouncedUpdateEligibilityHintAsync();
    }

    partial void OnEligibilityHintToneChanged(EligibilityHintTone value)
    {
        IsEligibilityHintPositive = value == EligibilityHintTone.Positive;
        IsEligibilityHintNegative = value == EligibilityHintTone.Negative;
        IsEligibilityHintWarning = value == EligibilityHintTone.Warning;
    }

    partial void OnOwnerNameChanged(string value)
    {
        if (_suppressOwnerSuggestionSearch)
        {
            return;
        }

        _ = DebouncedLoadOwnerSuggestionsAsync(value);
    }

    partial void OnIsExistingVehicleChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditVehicleNumber));

        if (value)
        {
            ClearOwnerSuggestions();
        }
    }

    [RelayCommand]
    private void ApplyOwnerSuggestion(OwnerSuggestion? suggestion)
    {
        if (suggestion is null)
        {
            return;
        }

        _ownerSuggestionDebounceCts?.Cancel();
        _suppressOwnerSuggestionSearch = true;
        OwnerName = suggestion.OwnerName;
        _suppressOwnerSuggestionSearch = false;

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            PhoneNumber = suggestion.PhoneNumber;
        }

        ReusableVehicles.Clear();
        foreach (var vehicle in suggestion.Vehicles)
        {
            ReusableVehicles.Add(vehicle);
        }

        HasReusableVehicles = ReusableVehicles.Count > 0;
        IsOwnerSuggestionsOpen = false;
    }

    [RelayCommand]
    private void ReuseVehicle(Vehicle? vehicle)
    {
        if (vehicle is null)
        {
            return;
        }

        VehicleNumberRaw = vehicle.VehicleNumberRaw;
        Brand = vehicle.Brand ?? string.Empty;
        Model = vehicle.Model ?? string.Empty;
        UpdateNormalizedVehicleNumber(VehicleNumberRaw);
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
                ServiceDate = ServiceDate?.Date ?? DateTime.Today
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

        if (ServiceDate is null)
        {
            return "Service date is required.";
        }

        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            return "Phone number is required.";
        }

        if (string.IsNullOrWhiteSpace(OwnerName))
        {
            return "Owner name is required.";
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

    private async Task DebouncedLoadOwnerSuggestionsAsync(string value)
    {
        _ownerSuggestionDebounceCts?.Cancel();
        _ownerSuggestionDebounceCts?.Dispose();
        _ownerSuggestionDebounceCts = new CancellationTokenSource();
        var cancellationToken = _ownerSuggestionDebounceCts.Token;

        if (IsExistingVehicle || _vehicleService is null || value.Trim().Length < 2)
        {
            ClearOwnerSuggestions();
            return;
        }

        try
        {
            await Task.Delay(250, cancellationToken);
            var query = value.Trim();
            var vehicles = await _vehicleService.SearchByOwnerNameAsync(query, cancellationToken);
            if (cancellationToken.IsCancellationRequested || !string.Equals(query, OwnerName.Trim(), StringComparison.Ordinal))
            {
                return;
            }

            var suggestions = vehicles
                .Where(vehicle => !string.IsNullOrWhiteSpace(vehicle.OwnerName))
                .GroupBy(vehicle => vehicle.OwnerName!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new OwnerSuggestion
                {
                    OwnerName = group.First().OwnerName!.Trim(),
                    PhoneNumber = group.FirstOrDefault(vehicle => !string.IsNullOrWhiteSpace(vehicle.PhoneNumber))?.PhoneNumber ?? string.Empty,
                    Vehicles = group.ToList()
                })
                .OrderBy(suggestion => suggestion.OwnerName)
                .ToList();

            OwnerSuggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                OwnerSuggestions.Add(suggestion);
            }

            IsOwnerSuggestionsOpen = OwnerSuggestions.Count > 0;
        }
        catch (OperationCanceledException)
        {
            // A newer owner-name lookup replaced this one.
        }
    }

    private async Task DebouncedUpdateEligibilityHintAsync()
    {
        _eligibilityDebounceCts?.Cancel();
        _eligibilityDebounceCts?.Dispose();
        _eligibilityDebounceCts = new CancellationTokenSource();
        var cancellationToken = _eligibilityDebounceCts.Token;

        if (_promotionUsageService is null ||
            SelectedPromotion is null ||
            string.IsNullOrWhiteSpace(NormalizedVehicleNumber) ||
            NormalizedVehicleNumber == "-")
        {
            EligibilityHint = "Choose a promotion and enter a vehicle number to preview eligibility.";
            EligibilityHintTone = EligibilityHintTone.Neutral;
            return;
        }

        try
        {
            await Task.Delay(300, cancellationToken);
            var result = await _promotionUsageService.CheckEligibilityAsync(
                NormalizedVehicleNumber,
                SelectedPromotion.Id,
                cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (result.IsEligible)
            {
                EligibilityHint = "Eligible — this vehicle can use this promotion.";
                EligibilityHintTone = EligibilityHintTone.Positive;
                return;
            }

            EligibilityHint = result.Message;
            EligibilityHintTone = result.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase)
                ? EligibilityHintTone.Warning
                : EligibilityHintTone.Negative;
        }
        catch (OperationCanceledException)
        {
            // A newer eligibility preview replaced this one.
        }
        catch (Exception exception)
        {
            EligibilityHint = $"Eligibility preview unavailable: {exception.Message}";
            EligibilityHintTone = EligibilityHintTone.Warning;
        }
    }

    private void ClearOwnerSuggestions()
    {
        OwnerSuggestions.Clear();
        IsOwnerSuggestionsOpen = false;
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

public enum EligibilityHintTone
{
    Neutral,
    Positive,
    Negative,
    Warning
}
