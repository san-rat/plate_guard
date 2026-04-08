using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Models;

namespace PlateGuard.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IVehicleService? _vehicleService;
    private readonly IPromotionService? _promotionService;
    private readonly IPromotionUsageService? _promotionUsageService;
    private readonly Dictionary<int, Promotion> _promotionLookup = [];
    private CancellationTokenSource? _searchDebounceCts;
    private bool _suppressSearchTextChanged;

    public event Action<AddUsageDialogRequest>? AddUsageRequested;

    public ObservableCollection<Promotion> ActivePromotions { get; } = [];
    public ObservableCollection<Vehicle> SearchResults { get; } = [];
    public ObservableCollection<UsageHistoryItemViewModel> SelectedVehicleHistory { get; } = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private Promotion? selectedPromotion;

    [ObservableProperty]
    private Vehicle? selectedVehicle;

    [ObservableProperty]
    private string statusMessage = "Loading promotions...";

    [ObservableProperty]
    private string searchModeLabel = "Search by vehicle number, phone number, or owner name";

    [ObservableProperty]
    private string resultsSummary = "Search results will appear here.";

    [ObservableProperty]
    private string emptyStateTitle = "Search to begin";

    [ObservableProperty]
    private string emptyStateMessage = "Search for a vehicle, phone number, or owner name.";

    [ObservableProperty]
    private string selectedVehicleNumber = "No vehicle selected";

    [ObservableProperty]
    private string selectedVehicleOwner = "-";

    [ObservableProperty]
    private string selectedVehiclePhone = "-";

    [ObservableProperty]
    private string selectedVehicleBrandModel = "-";

    [ObservableProperty]
    private string selectedPromotionName = "No active promotion selected";

    [ObservableProperty]
    private string selectedPromotionDetails = "Choose an active promotion to check vehicle eligibility.";

    [ObservableProperty]
    private string historySummary = "Select a vehicle to view promotion history.";

    [ObservableProperty]
    private string historyEmptyStateMessage = "No promotion usage records for this vehicle yet.";

    [ObservableProperty]
    private string eligibilityTitle = "Select a vehicle and promotion";

    [ObservableProperty]
    private string eligibilityMessage = "Search for a vehicle and choose an active promotion to check eligibility.";

    [ObservableProperty]
    private bool hasSearchResults;

    [ObservableProperty]
    private bool hasNoSearchResults = true;

    [ObservableProperty]
    private bool hasSelectedVehicle;

    [ObservableProperty]
    private bool hasHistory;

    [ObservableProperty]
    private bool hasNoHistory = true;

    [ObservableProperty]
    private bool canAddUsage;

    [ObservableProperty]
    private bool isBusy;

    public MainWindowViewModel()
    {
        // Design-time data only.
        ActivePromotions.Add(new Promotion { Id = 1, PromotionName = "Sample Promotion", IsActive = true });
        SelectedPromotion = ActivePromotions[0];
        SearchResults.Add(new Vehicle
        {
            Id = 1,
            VehicleNumberRaw = "CAB-1234",
            VehicleNumberNormalized = "CAB1234",
            OwnerName = "Nimal Perera",
            PhoneNumber = "0771234567",
            Brand = "Toyota",
            Model = "Corolla"
        });
        SelectedVehicle = SearchResults[0];
        SelectedVehicleHistory.Add(new UsageHistoryItemViewModel("New Year Promo", DateTime.Today, "5000.00"));
        HasSearchResults = true;
        HasNoSearchResults = false;
        HasSelectedVehicle = true;
        HasHistory = true;
        HasNoHistory = false;
        StatusMessage = "Design preview";
        ResultsSummary = "Found 1 matching vehicle.";
        EmptyStateTitle = "No matching vehicle found";
        EmptyStateMessage = "You can add this vehicle to the selected promotion if eligible.";
        SelectedPromotionName = "Sample Promotion";
        SelectedPromotionDetails = "Active now";
        HistorySummary = "1 promotion usage record for this vehicle.";
        EligibilityTitle = "Vehicle is eligible for this promotion";
        EligibilityMessage = "No previous usage found for this promotion.";
        CanAddUsage = true;
        UpdateSelectedVehicleSummary(SelectedVehicle);
    }

    public MainWindowViewModel(
        IVehicleService vehicleService,
        IPromotionService promotionService,
        IPromotionUsageService promotionUsageService)
    {
        _vehicleService = vehicleService;
        _promotionService = promotionService;
        _promotionUsageService = promotionUsageService;

        _ = InitializeAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_suppressSearchTextChanged)
        {
            return;
        }

        if (_vehicleService is null)
        {
            return;
        }

        _searchDebounceCts?.Cancel();

        if (string.IsNullOrWhiteSpace(value))
        {
            ClearSearchState();
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        _searchDebounceCts = cancellationTokenSource;

        _ = DebouncedSearchAsync(value, cancellationTokenSource.Token);
    }

    partial void OnSelectedVehicleChanged(Vehicle? value)
    {
        UpdateSelectedVehicleSummary(value);
        _ = RefreshSelectionStateAsync();
    }

    partial void OnSelectedPromotionChanged(Promotion? value)
    {
        UpdateSelectedPromotionSummary(value);
        _ = RefreshSelectionStateAsync();
    }

    [RelayCommand]
    private async Task RefreshPromotionsAsync()
    {
        if (_promotionService is null)
        {
            return;
        }

        await LoadPromotionsAsync();
        await RefreshSelectionStateAsync();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        ClearSearchState();
    }

    [RelayCommand]
    private void AddUsage()
    {
        if (!CanAddUsage || SelectedPromotion is null)
        {
            return;
        }

        var prefilledVehicleNumber = string.Empty;
        if (SelectedVehicle is not null)
        {
            prefilledVehicleNumber = SelectedVehicle.VehicleNumberRaw;
        }
        else if (!string.IsNullOrWhiteSpace(SearchText) && DetectSearchMode(SearchText) == SearchMode.VehicleNumber)
        {
            prefilledVehicleNumber = SearchText.Trim();
        }

        AddUsageRequested?.Invoke(new AddUsageDialogRequest
        {
            Vehicle = SelectedVehicle,
            SelectedPromotion = SelectedPromotion,
            AvailablePromotions = ActivePromotions.ToList(),
            PrefilledVehicleNumber = prefilledVehicleNumber
        });
    }

    private async Task InitializeAsync()
    {
        await LoadPromotionsAsync();
        ClearSearchState();
    }

    private async Task LoadPromotionsAsync()
    {
        if (_promotionService is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var previouslySelectedPromotionId = SelectedPromotion?.Id;
            var allPromotions = await _promotionService.GetAllAsync();
            _promotionLookup.Clear();
            foreach (var promotion in allPromotions)
            {
                _promotionLookup[promotion.Id] = promotion;
            }

            var activePromotions = allPromotions
                .Where(promotion => promotion.IsActive)
                .OrderBy(promotion => promotion.PromotionName)
                .ToList();

            ActivePromotions.Clear();
            foreach (var promotion in activePromotions)
            {
                ActivePromotions.Add(promotion);
            }

            SelectedPromotion = previouslySelectedPromotionId.HasValue
                ? ActivePromotions.FirstOrDefault(promotion => promotion.Id == previouslySelectedPromotionId.Value)
                : null;

            SelectedPromotion ??= ActivePromotions.FirstOrDefault();
            UpdateSelectedPromotionSummary(SelectedPromotion);
            StatusMessage = ActivePromotions.Count == 0
                ? "No active promotions configured. Create a promotion to start checking eligibility."
                : $"Loaded {ActivePromotions.Count} active promotion(s).";
        }
        catch (Exception exception)
        {
            ActivePromotions.Clear();
            SelectedPromotion = null;
            UpdateSelectedPromotionSummary(null);
            StatusMessage = $"Could not load promotions: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DebouncedSearchAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            await ExecuteSearchAsync(query, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A newer search request replaced this one.
        }
    }

    private async Task ExecuteSearchAsync(string rawQuery, CancellationToken cancellationToken)
    {
        if (_vehicleService is null)
        {
            return;
        }

        var query = rawQuery.Trim();
        if (string.IsNullOrEmpty(query))
        {
            ClearSearchState();
            return;
        }

        IsBusy = true;
        try
        {
            var searchMode = DetectSearchMode(query);
            SearchModeLabel = searchMode switch
            {
                SearchMode.VehicleNumber => "Searching by vehicle number",
                SearchMode.PhoneNumber => "Searching by phone number",
                _ => "Searching by owner name"
            };

            IReadOnlyList<Vehicle> results = searchMode switch
            {
                SearchMode.VehicleNumber => await SearchByVehicleNumberAsync(query, cancellationToken),
                SearchMode.PhoneNumber => await _vehicleService.SearchByPhoneNumberAsync(query, cancellationToken),
                _ => await _vehicleService.SearchByOwnerNameAsync(query, cancellationToken)
            };

            SearchResults.Clear();
            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            if (SearchResults.Count == 0)
            {
                HasSearchResults = false;
                HasNoSearchResults = true;
                SelectedVehicle = null;
                EmptyStateTitle = "No matching vehicle found";
                EmptyStateMessage = "You can add this vehicle to the selected promotion if eligible.";
                ResultsSummary = $"No matches for \"{query}\".";
                StatusMessage = "No matching vehicle found.";
                return;
            }

            HasSearchResults = true;
            HasNoSearchResults = false;
            EmptyStateTitle = string.Empty;
            EmptyStateMessage = string.Empty;
            ResultsSummary = $"Found {SearchResults.Count} matching vehicle(s).";
            StatusMessage = $"Found {SearchResults.Count} matching vehicle(s).";
            SelectedVehicle = SearchResults[0];
        }
        catch (OperationCanceledException)
        {
            // A newer search request replaced this one.
        }
        catch (Exception exception)
        {
            SearchResults.Clear();
            HasSearchResults = false;
            HasNoSearchResults = true;
            SelectedVehicle = null;
            EmptyStateTitle = "Search unavailable";
            EmptyStateMessage = "The search request failed. Review the database state and try again.";
            ResultsSummary = "Search did not complete.";
            StatusMessage = $"Search failed: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<IReadOnlyList<Vehicle>> SearchByVehicleNumberAsync(string query, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService!.FindByVehicleNumberAsync(query, cancellationToken);
        return vehicle is null ? [] : [vehicle];
    }

    private async Task RefreshSelectionStateAsync()
    {
        if (_promotionUsageService is null)
        {
            return;
        }

        try
        {
            SelectedVehicleHistory.Clear();

            if (SelectedVehicle is null)
            {
                HistorySummary = "Select a vehicle to view promotion history.";
                HistoryEmptyStateMessage = "No vehicle selected.";
                HasHistory = false;
                HasNoHistory = true;
                ApplyNoVehicleEligibilityState();
                return;
            }

            var history = await _promotionUsageService.GetUsageHistoryForVehicleAsync(SelectedVehicle.Id);
            foreach (var usage in history)
            {
                var promotionName = _promotionLookup.TryGetValue(usage.PromotionId, out var promotion)
                    ? promotion.PromotionName
                    : $"Promotion #{usage.PromotionId}";

                var amountPaidText = usage.AmountPaid.HasValue ? usage.AmountPaid.Value.ToString("0.00") : "-";
                SelectedVehicleHistory.Add(new UsageHistoryItemViewModel(promotionName, usage.ServiceDate, amountPaidText));
            }

            HasHistory = SelectedVehicleHistory.Count > 0;
            HasNoHistory = !HasHistory;
            HistorySummary = HasHistory
                ? $"{SelectedVehicleHistory.Count} promotion usage record(s) for this vehicle."
                : "No promotion usage records for this vehicle yet.";
            HistoryEmptyStateMessage = "This vehicle has not used any tracked promotion yet.";

            if (SelectedPromotion is null)
            {
                EligibilityTitle = "Select a promotion";
                EligibilityMessage = "Choose an active promotion to check eligibility.";
                CanAddUsage = false;
                return;
            }

            var eligibility = await _promotionUsageService.CheckEligibilityAsync(SelectedVehicle.Id, SelectedPromotion.Id);
            EligibilityTitle = eligibility.IsEligible
                ? "Vehicle is eligible for this promotion"
                : "Promotion already used or unavailable";
            EligibilityMessage = eligibility.Message;
            CanAddUsage = eligibility.IsEligible;
        }
        catch (Exception exception)
        {
            SelectedVehicleHistory.Clear();
            HasHistory = false;
            HasNoHistory = true;
            HistorySummary = "Promotion history is unavailable right now.";
            HistoryEmptyStateMessage = "The history request failed.";
            EligibilityTitle = "Eligibility check unavailable";
            EligibilityMessage = "Review the database state and try again.";
            CanAddUsage = false;
            StatusMessage = $"Could not refresh selection details: {exception.Message}";
        }
    }

    public async Task RefreshAfterUsageSavedAsync(SavePromotionUsageResult result)
    {
        StatusMessage = result.Message;

        if (result.PromotionUsage is not null)
        {
            var matchingPromotion = ActivePromotions.FirstOrDefault(promotion => promotion.Id == result.PromotionUsage.PromotionId);
            if (matchingPromotion is not null)
            {
                SelectedPromotion = matchingPromotion;
            }
        }

        if (result.Vehicle is null)
        {
            await RefreshSelectionStateAsync();
            return;
        }

        var query = result.Vehicle.VehicleNumberRaw;

        _searchDebounceCts?.Cancel();
        _suppressSearchTextChanged = true;
        SearchText = query;
        _suppressSearchTextChanged = false;

        await ExecuteSearchAsync(query, CancellationToken.None);

        SelectedVehicle = SearchResults.FirstOrDefault(vehicle => vehicle.Id == result.Vehicle.Id)
            ?? SearchResults.FirstOrDefault();

        StatusMessage = result.Message;
    }

    private void ClearSearchState()
    {
        SearchResults.Clear();
        HasSearchResults = false;
        HasNoSearchResults = true;
        SelectedVehicle = null;
        SelectedVehicleHistory.Clear();
        HasHistory = false;
        HasNoHistory = true;
        SearchModeLabel = "Search by vehicle number, phone number, or owner name";
        ResultsSummary = "Search results will appear here.";
        EmptyStateTitle = "Search to begin";
        EmptyStateMessage = "Search for a vehicle, phone number, or owner name.";
        StatusMessage = ActivePromotions.Count == 0
            ? "No active promotions configured. Create a promotion to start checking eligibility."
            : "Search for a vehicle to check promotion eligibility.";
        HistorySummary = "Select a vehicle to view promotion history.";
        HistoryEmptyStateMessage = "No vehicle selected.";
        UpdateSelectedVehicleSummary(null);
        ApplyNoVehicleEligibilityState();
    }

    private void UpdateSelectedVehicleSummary(Vehicle? vehicle)
    {
        HasSelectedVehicle = vehicle is not null;

        if (vehicle is null)
        {
            SelectedVehicleNumber = "No vehicle selected";
            SelectedVehicleOwner = "-";
            SelectedVehiclePhone = "-";
            SelectedVehicleBrandModel = "-";
            return;
        }

        SelectedVehicleNumber = FormatVehicleNumber(vehicle);
        SelectedVehicleOwner = string.IsNullOrWhiteSpace(vehicle.OwnerName) ? "-" : vehicle.OwnerName.Trim();
        SelectedVehiclePhone = string.IsNullOrWhiteSpace(vehicle.PhoneNumber) ? "-" : vehicle.PhoneNumber.Trim();
        SelectedVehicleBrandModel = string.IsNullOrWhiteSpace(vehicle.Brand) && string.IsNullOrWhiteSpace(vehicle.Model)
            ? "-"
            : $"{vehicle.Brand} {vehicle.Model}".Trim();
    }

    private void UpdateSelectedPromotionSummary(Promotion? promotion)
    {
        if (promotion is null)
        {
            SelectedPromotionName = "No active promotion selected";
            SelectedPromotionDetails = "Choose an active promotion to check vehicle eligibility.";
            return;
        }

        SelectedPromotionName = promotion.PromotionName;

        var detailParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(promotion.Description))
        {
            detailParts.Add(promotion.Description.Trim());
        }

        var dateWindow = FormatPromotionWindow(promotion);
        if (!string.IsNullOrEmpty(dateWindow))
        {
            detailParts.Add(dateWindow);
        }

        detailParts.Add(promotion.IsActive ? "Active now" : "Inactive");
        SelectedPromotionDetails = string.Join(" | ", detailParts);
    }

    private void ApplyNoVehicleEligibilityState()
    {
        if (SelectedPromotion is null)
        {
            EligibilityTitle = "Select a promotion";
            EligibilityMessage = "Choose an active promotion before adding or checking usage.";
            CanAddUsage = false;
            return;
        }

        if (!SelectedPromotion.IsActive)
        {
            EligibilityTitle = "Selected promotion is inactive";
            EligibilityMessage = "Activate the promotion before recording a new usage.";
            CanAddUsage = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            EligibilityTitle = "Select a vehicle and promotion";
            EligibilityMessage = "Search for a vehicle and choose an active promotion to check eligibility.";
            CanAddUsage = false;
            return;
        }

        var searchMode = DetectSearchMode(SearchText);
        if (searchMode != SearchMode.VehicleNumber)
        {
            EligibilityTitle = "Search by vehicle number to add a new vehicle";
            EligibilityMessage = "Phone and owner searches are for finding existing vehicles. Use a vehicle number search to create a new vehicle usage record.";
            CanAddUsage = false;
            return;
        }

        EligibilityTitle = "New vehicle can be added for this promotion";
        EligibilityMessage = "No existing vehicle matched this vehicle number. Use Add Usage to create the vehicle record and save the promotion usage.";
        CanAddUsage = true;
    }

    private static string FormatVehicleNumber(Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.VehicleNumberRaw))
        {
            return vehicle.VehicleNumberNormalized;
        }

        var raw = vehicle.VehicleNumberRaw.Trim();
        return string.Equals(raw, vehicle.VehicleNumberNormalized, StringComparison.OrdinalIgnoreCase)
            ? raw
            : $"{raw} ({vehicle.VehicleNumberNormalized})";
    }

    private static string FormatPromotionWindow(Promotion promotion)
    {
        return (promotion.StartDate, promotion.EndDate) switch
        {
            (DateTime startDate, DateTime endDate) => $"Valid {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            (DateTime startDate, null) => $"Starts {startDate:yyyy-MM-dd}",
            (null, DateTime endDate) => $"Ends {endDate:yyyy-MM-dd}",
            _ => string.Empty
        };
    }

    private static SearchMode DetectSearchMode(string query)
    {
        var trimmed = query.Trim();
        var letterCount = trimmed.Count(char.IsLetter);
        var digitCount = trimmed.Count(char.IsDigit);
        var phoneLike = trimmed.All(character => char.IsDigit(character) || character is '+' or '-' or ' ');

        if (letterCount > 0 && digitCount > 0)
        {
            return SearchMode.VehicleNumber;
        }

        if (phoneLike && digitCount > 0)
        {
            return SearchMode.PhoneNumber;
        }

        return SearchMode.OwnerName;
    }

    private enum SearchMode
    {
        VehicleNumber,
        PhoneNumber,
        OwnerName
    }
}
