using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
    private readonly ISettingsService? _settingsService;
    private readonly IExportService? _exportService;
    private readonly Dictionary<int, Promotion> _promotionLookup = [];
    private AppSettings? _loadedSettings;
    private CancellationTokenSource? _searchDebounceCts;
    private bool _suppressSearchTextChanged;

    public event Action<AddUsageDialogRequest>? AddUsageRequested;
    public event Action<PromotionDialogRequest>? PromotionDialogRequested;
    public event Action<EditUsageDialogRequest>? EditUsageRequested;
    public event Action<DeleteUsageDialogRequest>? DeleteUsageRequested;

    public ObservableCollection<Promotion> ActivePromotions { get; } = [];
    public ObservableCollection<PromotionManagementItemViewModel> PromotionManagementItems { get; } = [];
    public ObservableCollection<Vehicle> SearchResults { get; } = [];
    public ObservableCollection<UsageHistoryItemViewModel> SelectedVehicleHistory { get; } = [];
    public ObservableCollection<HistoryPromotionFilterOptionViewModel> HistoryPromotionFilters { get; } = [];
    public ObservableCollection<HistoryRecordItemViewModel> HistoryRecords { get; } = [];

    [ObservableProperty]
    private bool isSearchSectionVisible = true;

    [ObservableProperty]
    private bool isPromotionsSectionVisible;

    [ObservableProperty]
    private bool isHistorySectionVisible;

    [ObservableProperty]
    private bool isSettingsSectionVisible;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private Promotion? selectedPromotion;

    [ObservableProperty]
    private Vehicle? selectedVehicle;

    [ObservableProperty]
    private string statusMessage = "Loading promotions...";

    [ObservableProperty]
    private string shopNameDisplay = "Shop name not set";

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
    private bool isEligibilityPositive;

    [ObservableProperty]
    private bool isEligibilityWarning;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private PromotionManagementItemViewModel? selectedPromotionManagementItem;

    [ObservableProperty]
    private string promotionManagementSummary = "Promotions will appear here.";

    [ObservableProperty]
    private string promotionManagementStatusMessage = "Promotion management is ready.";

    [ObservableProperty]
    private string promotionManagementEmptyTitle = "No promotions yet";

    [ObservableProperty]
    private string promotionManagementEmptyMessage = "Create a promotion to start tracking promotion usage.";

    [ObservableProperty]
    private string selectedManagementPromotionName = "No promotion selected";

    [ObservableProperty]
    private string selectedManagementPromotionDescription = "-";

    [ObservableProperty]
    private string selectedManagementPromotionStatus = "-";

    [ObservableProperty]
    private string selectedManagementPromotionWindow = "-";

    [ObservableProperty]
    private string selectedManagementPromotionUsageCount = "-";

    [ObservableProperty]
    private bool hasPromotionManagementItems;

    [ObservableProperty]
    private bool hasNoPromotionManagementItems = true;

    [ObservableProperty]
    private bool canEditSelectedPromotion;

    [ObservableProperty]
    private string togglePromotionStatusButtonText = "Deactivate";

    [ObservableProperty]
    private HistoryPromotionFilterOptionViewModel? selectedHistoryPromotionFilter;

    [ObservableProperty]
    private HistoryRecordItemViewModel? selectedHistoryRecord;

    [ObservableProperty]
    private string historySearchText = string.Empty;

    [ObservableProperty]
    private string historyDateFromText = string.Empty;

    [ObservableProperty]
    private string historyDateToText = string.Empty;

    [ObservableProperty]
    private string historyRecordsSummary = "Records will appear here.";

    [ObservableProperty]
    private string historyRecordsStatusMessage = "Review and maintain promotion records here.";

    [ObservableProperty]
    private string settingsShopName = string.Empty;

    [ObservableProperty]
    private string settingsExportFolder = string.Empty;

    [ObservableProperty]
    private string settingsStatusMessage = "Configure shop name, export folder, and delete password here.";

    [ObservableProperty]
    private string currentDeletePassword = string.Empty;

    [ObservableProperty]
    private string newDeletePassword = string.Empty;

    [ObservableProperty]
    private string confirmDeletePassword = string.Empty;

    [ObservableProperty]
    private string historyRecordsEmptyTitle = "No records yet";

    [ObservableProperty]
    private string historyRecordsEmptyMessage = "Add usage records to review and manage them here.";

    [ObservableProperty]
    private bool hasHistoryRecords;

    [ObservableProperty]
    private bool hasNoHistoryRecords = true;

    [ObservableProperty]
    private string selectedHistoryVehicleNumber = "No record selected";

    [ObservableProperty]
    private string selectedHistoryPromotionName = "-";

    [ObservableProperty]
    private string selectedHistoryServiceDate = "-";

    [ObservableProperty]
    private string selectedHistoryOwner = "-";

    [ObservableProperty]
    private string selectedHistoryPhone = "-";

    [ObservableProperty]
    private string selectedHistoryBrandModel = "-";

    [ObservableProperty]
    private string selectedHistoryAmountPaid = "-";

    [ObservableProperty]
    private string selectedHistoryMileage = "-";

    [ObservableProperty]
    private string selectedHistoryPricing = "-";

    [ObservableProperty]
    private string selectedHistoryNotes = "-";

    [ObservableProperty]
    private string selectedHistoryRecordStatus = "-";

    [ObservableProperty]
    private bool canEditSelectedHistoryRecord;

    [ObservableProperty]
    private bool canDeleteSelectedHistoryRecord;

    public string SettingsExportFolderHint => "Leave blank to use the default Documents/PlateGuard Exports folder.";

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
        SetEligibilityState(
            "Vehicle is eligible for this promotion",
            "No previous usage found for this promotion.",
            canAddUsage: true,
            EligibilityDisplayTone.Positive);
        PromotionManagementItems.Add(new PromotionManagementItemViewModel(
            new Promotion
            {
                Id = 1,
                PromotionName = "Sample Promotion",
                Description = "New Year offer",
                IsActive = true,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30)
            },
            12));
        PromotionManagementItems.Add(new PromotionManagementItemViewModel(
            new Promotion
            {
                Id = 2,
                PromotionName = "Weekend Wash",
                Description = "Inactive sample",
                IsActive = false
            },
            3));
        HasPromotionManagementItems = true;
        HasNoPromotionManagementItems = false;
        PromotionManagementSummary = "2 promotions configured.";
        PromotionManagementStatusMessage = "Select a promotion to edit or toggle status.";
        SelectedPromotionManagementItem = PromotionManagementItems[0];
        HistoryPromotionFilters.Add(new HistoryPromotionFilterOptionViewModel(null, "All Promotions"));
        HistoryPromotionFilters.Add(new HistoryPromotionFilterOptionViewModel(1, "Sample Promotion"));
        SelectedHistoryPromotionFilter = HistoryPromotionFilters[0];
        HistoryRecords.Add(new HistoryRecordItemViewModel(new PromotionUsageRecord
        {
            PromotionUsageId = 1,
            VehicleId = 1,
            PromotionId = 1,
            ServiceDate = DateTime.Today,
            VehicleNumberRaw = "CAB-1234",
            VehicleNumberNormalized = "CAB1234",
            PhoneNumber = "0771234567",
            OwnerName = "Nimal Perera",
            Brand = "Toyota",
            Model = "Corolla",
            PromotionName = "Sample Promotion",
            PromotionIsActive = true,
            Mileage = 45000,
            NormalPrice = 7500,
            DiscountedPrice = 5000,
            AmountPaid = 5000,
            Notes = "Design preview"
        }));
        SelectedHistoryRecord = HistoryRecords[0];
        HasHistoryRecords = true;
        HasNoHistoryRecords = false;
        HistoryRecordsSummary = "1 record available.";
        HistoryRecordsStatusMessage = "Select a record to edit or delete it.";
        ShopNameDisplay = "Sample Service Center";
        SettingsShopName = "Sample Service Center";
        SettingsExportFolder = @"C:\Exports\PlateGuard";
        SettingsStatusMessage = "Settings are ready.";
        UpdateSelectedVehicleSummary(SelectedVehicle);
        UpdateSelectedManagementPromotionSummary(SelectedPromotionManagementItem);
        UpdateSelectedHistoryRecordSummary(SelectedHistoryRecord);
    }

    public MainWindowViewModel(
        IVehicleService vehicleService,
        IPromotionService promotionService,
        IPromotionUsageService promotionUsageService,
        ISettingsService settingsService,
        IExportService exportService)
    {
        _vehicleService = vehicleService;
        _promotionService = promotionService;
        _promotionUsageService = promotionUsageService;
        _settingsService = settingsService;
        _exportService = exportService;

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

    partial void OnSelectedPromotionManagementItemChanged(PromotionManagementItemViewModel? value)
    {
        UpdateSelectedManagementPromotionSummary(value);
    }

    partial void OnSelectedHistoryRecordChanged(HistoryRecordItemViewModel? value)
    {
        UpdateSelectedHistoryRecordSummary(value);
    }

    [RelayCommand]
    private async Task RefreshPromotionsAsync()
    {
        if (_promotionService is null)
        {
            return;
        }

        await LoadPromotionsAsync();
        await LoadPromotionManagementAsync();
        await LoadHistoryRecordsAsync();
        await RefreshSelectionStateAsync();
    }

    [RelayCommand]
    private void ShowSearchSection()
    {
        IsSearchSectionVisible = true;
        IsPromotionsSectionVisible = false;
        IsHistorySectionVisible = false;
        IsSettingsSectionVisible = false;
    }

    [RelayCommand]
    private async Task ShowPromotionsSectionAsync()
    {
        IsSearchSectionVisible = false;
        IsPromotionsSectionVisible = true;
        IsHistorySectionVisible = false;
        IsSettingsSectionVisible = false;
        await LoadPromotionManagementAsync();
    }

    [RelayCommand]
    private async Task ShowHistorySectionAsync()
    {
        IsSearchSectionVisible = false;
        IsPromotionsSectionVisible = false;
        IsHistorySectionVisible = true;
        IsSettingsSectionVisible = false;
        await LoadHistoryRecordsAsync();
    }

    [RelayCommand]
    private async Task ShowSettingsSectionAsync()
    {
        IsSearchSectionVisible = false;
        IsPromotionsSectionVisible = false;
        IsHistorySectionVisible = false;
        IsSettingsSectionVisible = true;
        await LoadSettingsAsync();
    }

    [RelayCommand]
    private async Task RefreshPromotionManagementAsync()
    {
        await LoadPromotionManagementAsync();
    }

    [RelayCommand]
    private async Task RefreshHistoryRecordsAsync()
    {
        await LoadHistoryRecordsAsync();
    }

    [RelayCommand]
    private async Task ExportFilteredHistoryRecordsAsync()
    {
        await ExportHistoryRecordsAsync(useCurrentFilters: true);
    }

    [RelayCommand]
    private async Task ExportAllHistoryRecordsAsync()
    {
        await ExportHistoryRecordsAsync(useCurrentFilters: false);
    }

    [RelayCommand]
    private async Task ClearHistoryFiltersAsync()
    {
        HistorySearchText = string.Empty;
        HistoryDateFromText = string.Empty;
        HistoryDateToText = string.Empty;
        SelectedHistoryPromotionFilter = HistoryPromotionFilters.FirstOrDefault();
        await LoadHistoryRecordsAsync();
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (_settingsService is null)
        {
            SettingsStatusMessage = "Settings are not available in design mode.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _settingsService.UpdateAsync(new UpdateAppSettingsRequest
            {
                ShopName = SettingsShopName,
                ExportFolder = SettingsExportFolder
            });

            SettingsStatusMessage = result.Message;
            if (result.IsSuccess && result.Settings is not null)
            {
                ApplyLoadedSettings(result.Settings);
            }
        }
        catch (Exception exception)
        {
            SettingsStatusMessage = $"Could not save settings: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ChangeDeletePasswordAsync()
    {
        if (_settingsService is null)
        {
            SettingsStatusMessage = "Settings are not available in design mode.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _settingsService.ChangeDeletePasswordAsync(new ChangeDeletePasswordRequest
            {
                CurrentPassword = CurrentDeletePassword,
                NewPassword = NewDeletePassword,
                ConfirmNewPassword = ConfirmDeletePassword
            });

            SettingsStatusMessage = result.Message;
            if (result.IsSuccess)
            {
                CurrentDeletePassword = string.Empty;
                NewDeletePassword = string.Empty;
                ConfirmDeletePassword = string.Empty;
                ApplyLoadedSettings(await _settingsService.GetAsync());
            }
        }
        catch (Exception exception)
        {
            SettingsStatusMessage = $"Could not change the delete password: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddPromotion()
    {
        PromotionDialogRequested?.Invoke(new PromotionDialogRequest());
    }

    [RelayCommand]
    private void EditSelectedPromotion()
    {
        if (SelectedPromotionManagementItem is null)
        {
            return;
        }

        PromotionDialogRequested?.Invoke(new PromotionDialogRequest
        {
            Promotion = SelectedPromotionManagementItem.Promotion
        });
    }

    [RelayCommand]
    private async Task ToggleSelectedPromotionStatusAsync()
    {
        if (_promotionService is null || SelectedPromotionManagementItem is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var promotion = SelectedPromotionManagementItem.Promotion;
            var updatedPromotion = promotion.IsActive
                ? await _promotionService.DeactivateAsync(promotion.Id)
                : await _promotionService.ActivateAsync(promotion.Id);

            await LoadPromotionsAsync();
            await LoadPromotionManagementAsync(updatedPromotion.Id);
            await RefreshSelectionStateAsync();

            PromotionManagementStatusMessage = updatedPromotion.IsActive
                ? $"Activated promotion \"{updatedPromotion.PromotionName}\"."
                : $"Deactivated promotion \"{updatedPromotion.PromotionName}\".";
        }
        catch (Exception exception)
        {
            PromotionManagementStatusMessage = $"Could not update promotion status: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void EditSelectedHistoryRecord()
    {
        if (SelectedHistoryRecord is null)
        {
            return;
        }

        EditUsageRequested?.Invoke(new EditUsageDialogRequest
        {
            Record = SelectedHistoryRecord.Record
        });
    }

    [RelayCommand]
    private void DeleteSelectedHistoryRecord()
    {
        if (SelectedHistoryRecord is null)
        {
            return;
        }

        DeleteUsageRequested?.Invoke(new DeleteUsageDialogRequest
        {
            Record = SelectedHistoryRecord.Record
        });
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
        await LoadPromotionManagementAsync();
        await LoadHistoryRecordsAsync();
        await LoadSettingsAsync();
        ClearSearchState();
    }

    private async Task LoadSettingsAsync()
    {
        if (_settingsService is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var settings = await _settingsService.GetAsync();
            ApplyLoadedSettings(settings);
            SettingsStatusMessage = "Settings loaded.";
        }
        catch (Exception exception)
        {
            SettingsStatusMessage = $"Could not load settings: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportHistoryRecordsAsync(bool useCurrentFilters)
    {
        if (_promotionUsageService is null || _exportService is null)
        {
            HistoryRecordsStatusMessage = "CSV export is not available in design mode.";
            return;
        }

        PromotionUsageRecordQuery query;
        if (useCurrentFilters)
        {
            var validationMessage = TryBuildHistoryQuery(out query);
            if (validationMessage is not null)
            {
                HistoryRecordsStatusMessage = validationMessage;
                return;
            }
        }
        else
        {
            query = new PromotionUsageRecordQuery();
        }

        IsBusy = true;
        try
        {
            var records = await _promotionUsageService.SearchUsageRecordsAsync(query);
            var result = await _exportService.ExportPromotionUsageRecordsAsync(new ExportPromotionUsageRecordsRequest
            {
                Records = records,
                ExportFolder = _loadedSettings?.ExportFolder,
                FileNamePrefix = useCurrentFilters ? "plateguard-history-filtered" : "plateguard-history-all"
            });

            HistoryRecordsStatusMessage = result.Message;
        }
        catch (Exception exception)
        {
            HistoryRecordsStatusMessage = $"Could not export records: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyLoadedSettings(AppSettings settings)
    {
        _loadedSettings = settings;
        SettingsShopName = settings.ShopName ?? string.Empty;
        SettingsExportFolder = settings.ExportFolder ?? string.Empty;
        ShopNameDisplay = string.IsNullOrWhiteSpace(settings.ShopName)
            ? "Shop name not set"
            : settings.ShopName.Trim();
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
            UpdateHistoryPromotionFilters();
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
                SetEligibilityState(
                    "Select a promotion",
                    "Choose an active promotion to check eligibility.",
                    canAddUsage: false,
                    EligibilityDisplayTone.Neutral);
                return;
            }

            var eligibility = await _promotionUsageService.CheckEligibilityAsync(SelectedVehicle.Id, SelectedPromotion.Id);
            SetEligibilityState(
                eligibility.IsEligible
                    ? "Vehicle is eligible for this promotion"
                    : "Promotion already used or unavailable",
                eligibility.Message,
                eligibility.IsEligible,
                eligibility.IsEligible ? EligibilityDisplayTone.Positive : EligibilityDisplayTone.Warning);
        }
        catch (Exception exception)
        {
            SelectedVehicleHistory.Clear();
            HasHistory = false;
            HasNoHistory = true;
            HistorySummary = "Promotion history is unavailable right now.";
            HistoryEmptyStateMessage = "The history request failed.";
            SetEligibilityState(
                "Eligibility check unavailable",
                "Review the database state and try again.",
                canAddUsage: false,
                EligibilityDisplayTone.Warning);
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

    public async Task RefreshAfterPromotionSavedAsync(Promotion promotion)
    {
        await LoadPromotionsAsync();
        await LoadPromotionManagementAsync(promotion.Id);
        await LoadHistoryRecordsAsync();
        await RefreshSelectionStateAsync();

        PromotionManagementStatusMessage = $"Saved promotion \"{promotion.PromotionName}\" successfully.";
    }

    public async Task RefreshAfterUsageRecordUpdatedAsync()
    {
        await LoadHistoryRecordsAsync(SelectedHistoryRecord?.PromotionUsageId);
        await RefreshCurrentSearchAsync();
        await RefreshSelectionStateAsync();
        HistoryRecordsStatusMessage = "Record updated successfully.";
    }

    public async Task RefreshAfterUsageDeletedAsync()
    {
        await LoadHistoryRecordsAsync();
        await LoadPromotionManagementAsync();
        await RefreshCurrentSearchAsync();
        await RefreshSelectionStateAsync();
        HistoryRecordsStatusMessage = "Record deleted successfully.";
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

    private async Task LoadPromotionManagementAsync(int? preferredPromotionId = null)
    {
        if (_promotionService is null || _promotionUsageService is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var previousSelectionId = preferredPromotionId ?? SelectedPromotionManagementItem?.PromotionId;
            var promotions = await _promotionService.GetAllAsync();

            PromotionManagementItems.Clear();
            foreach (var promotion in promotions)
            {
                var usageCount = await _promotionUsageService.GetUsageCountForPromotionAsync(promotion.Id);
                PromotionManagementItems.Add(new PromotionManagementItemViewModel(promotion, usageCount));
            }

            HasPromotionManagementItems = PromotionManagementItems.Count > 0;
            HasNoPromotionManagementItems = !HasPromotionManagementItems;
            PromotionManagementSummary = HasPromotionManagementItems
                ? $"{PromotionManagementItems.Count} promotion(s) configured."
                : "No promotions configured yet.";

            if (!HasPromotionManagementItems)
            {
                SelectedPromotionManagementItem = null;
                UpdateSelectedManagementPromotionSummary(null);
                PromotionManagementStatusMessage = "Create the first promotion to start using PlateGuard.";
                return;
            }

            SelectedPromotionManagementItem = previousSelectionId.HasValue
                ? PromotionManagementItems.FirstOrDefault(item => item.PromotionId == previousSelectionId.Value)
                : null;

            SelectedPromotionManagementItem ??= PromotionManagementItems.FirstOrDefault();
            PromotionManagementStatusMessage = "Select a promotion to edit details or change active status.";
        }
        catch (Exception exception)
        {
            PromotionManagementItems.Clear();
            HasPromotionManagementItems = false;
            HasNoPromotionManagementItems = true;
            SelectedPromotionManagementItem = null;
            UpdateSelectedManagementPromotionSummary(null);
            PromotionManagementSummary = "Promotion list unavailable.";
            PromotionManagementStatusMessage = $"Could not load promotions: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadHistoryRecordsAsync(int? preferredPromotionUsageId = null)
    {
        if (_promotionUsageService is null)
        {
            return;
        }

        var dateValidationMessage = TryBuildHistoryQuery(out var query);
        if (dateValidationMessage is not null)
        {
            HistoryRecords.Clear();
            HasHistoryRecords = false;
            HasNoHistoryRecords = true;
            SelectedHistoryRecord = null;
            UpdateSelectedHistoryRecordSummary(null);
            HistoryRecordsSummary = "History filter is invalid.";
            HistoryRecordsStatusMessage = dateValidationMessage;
            return;
        }

        IsBusy = true;
        try
        {
            var previousSelectionId = preferredPromotionUsageId ?? SelectedHistoryRecord?.PromotionUsageId;
            var records = await _promotionUsageService.SearchUsageRecordsAsync(query);

            HistoryRecords.Clear();
            foreach (var record in records)
            {
                HistoryRecords.Add(new HistoryRecordItemViewModel(record));
            }

            HasHistoryRecords = HistoryRecords.Count > 0;
            HasNoHistoryRecords = !HasHistoryRecords;
            HistoryRecordsSummary = HasHistoryRecords
                ? $"{HistoryRecords.Count} record(s) found."
                : "No records matched the current filters.";

            if (!HasHistoryRecords)
            {
                SelectedHistoryRecord = null;
                UpdateSelectedHistoryRecordSummary(null);
                HistoryRecordsStatusMessage = "Adjust the filters or add usage records.";
                return;
            }

            SelectedHistoryRecord = previousSelectionId.HasValue
                ? HistoryRecords.FirstOrDefault(record => record.PromotionUsageId == previousSelectionId.Value)
                : null;

            SelectedHistoryRecord ??= HistoryRecords.FirstOrDefault();
            HistoryRecordsStatusMessage = "Select a record to review, edit, or delete it.";
        }
        catch (Exception exception)
        {
            HistoryRecords.Clear();
            HasHistoryRecords = false;
            HasNoHistoryRecords = true;
            SelectedHistoryRecord = null;
            UpdateSelectedHistoryRecordSummary(null);
            HistoryRecordsSummary = "History is unavailable.";
            HistoryRecordsStatusMessage = $"Could not load records: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateSelectedManagementPromotionSummary(PromotionManagementItemViewModel? item)
    {
        if (item is null)
        {
            SelectedManagementPromotionName = "No promotion selected";
            SelectedManagementPromotionDescription = "-";
            SelectedManagementPromotionStatus = "-";
            SelectedManagementPromotionWindow = "-";
            SelectedManagementPromotionUsageCount = "-";
            CanEditSelectedPromotion = false;
            TogglePromotionStatusButtonText = "Deactivate";
            return;
        }

        SelectedManagementPromotionName = item.PromotionName;
        SelectedManagementPromotionDescription = string.IsNullOrWhiteSpace(item.Description) ? "-" : item.Description;
        SelectedManagementPromotionStatus = item.StatusText;
        SelectedManagementPromotionWindow = item.ScheduleText;
        SelectedManagementPromotionUsageCount = item.UsageCountText;
        CanEditSelectedPromotion = true;
        TogglePromotionStatusButtonText = item.IsActive ? "Deactivate" : "Activate";
    }

    private void UpdateSelectedHistoryRecordSummary(HistoryRecordItemViewModel? item)
    {
        if (item is null)
        {
            SelectedHistoryVehicleNumber = "No record selected";
            SelectedHistoryPromotionName = "-";
            SelectedHistoryServiceDate = "-";
            SelectedHistoryOwner = "-";
            SelectedHistoryPhone = "-";
            SelectedHistoryBrandModel = "-";
            SelectedHistoryAmountPaid = "-";
            SelectedHistoryMileage = "-";
            SelectedHistoryPricing = "-";
            SelectedHistoryNotes = "-";
            SelectedHistoryRecordStatus = "-";
            CanEditSelectedHistoryRecord = false;
            CanDeleteSelectedHistoryRecord = false;
            return;
        }

        SelectedHistoryVehicleNumber = item.VehicleNumberDisplay;
        SelectedHistoryPromotionName = item.PromotionName;
        SelectedHistoryServiceDate = item.ServiceDateText;
        SelectedHistoryOwner = item.OwnerName;
        SelectedHistoryPhone = item.PhoneNumber;
        SelectedHistoryBrandModel = item.BrandModelText;
        SelectedHistoryAmountPaid = item.AmountPaidText;
        SelectedHistoryMileage = item.MileageText;
        SelectedHistoryPricing = item.PricingText;
        SelectedHistoryNotes = item.NotesText;
        SelectedHistoryRecordStatus = item.StatusText;
        CanEditSelectedHistoryRecord = true;
        CanDeleteSelectedHistoryRecord = true;
    }

    private void UpdateHistoryPromotionFilters()
    {
        var previousPromotionId = SelectedHistoryPromotionFilter?.PromotionId;
        HistoryPromotionFilters.Clear();
        HistoryPromotionFilters.Add(new HistoryPromotionFilterOptionViewModel(null, "All Promotions"));

        foreach (var promotion in _promotionLookup.Values.OrderBy(promotion => promotion.PromotionName))
        {
            HistoryPromotionFilters.Add(new HistoryPromotionFilterOptionViewModel(promotion.Id, promotion.PromotionName));
        }

        SelectedHistoryPromotionFilter = HistoryPromotionFilters.FirstOrDefault(filter => filter.PromotionId == previousPromotionId)
            ?? HistoryPromotionFilters.FirstOrDefault();
    }

    private string? TryBuildHistoryQuery(out PromotionUsageRecordQuery query)
    {
        query = new PromotionUsageRecordQuery
        {
            SearchText = string.IsNullOrWhiteSpace(HistorySearchText) ? null : HistorySearchText.Trim(),
            PromotionId = SelectedHistoryPromotionFilter?.PromotionId
        };

        if (!TryParseOptionalFilterDate(HistoryDateFromText, out var dateFrom))
        {
            return "From date must be a valid date in yyyy-MM-dd format.";
        }

        if (!TryParseOptionalFilterDate(HistoryDateToText, out var dateTo))
        {
            return "To date must be a valid date in yyyy-MM-dd format.";
        }

        if (dateFrom.HasValue && dateTo.HasValue && dateTo.Value.Date < dateFrom.Value.Date)
        {
            return "To date cannot be earlier than from date.";
        }

        query.DateFrom = dateFrom;
        query.DateTo = dateTo;
        return null;
    }

    private async Task RefreshCurrentSearchAsync()
    {
        if (_vehicleService is null || string.IsNullOrWhiteSpace(SearchText))
        {
            return;
        }

        await ExecuteSearchAsync(SearchText, CancellationToken.None);
    }

    private void ApplyNoVehicleEligibilityState()
    {
        if (SelectedPromotion is null)
        {
            SetEligibilityState(
                "Select a promotion",
                "Choose an active promotion before adding or checking usage.",
                canAddUsage: false,
                EligibilityDisplayTone.Neutral);
            return;
        }

        if (!SelectedPromotion.IsActive)
        {
            SetEligibilityState(
                "Selected promotion is inactive",
                "Activate the promotion before recording a new usage.",
                canAddUsage: false,
                EligibilityDisplayTone.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SetEligibilityState(
                "Select a vehicle and promotion",
                "Search for a vehicle and choose an active promotion to check eligibility.",
                canAddUsage: false,
                EligibilityDisplayTone.Neutral);
            return;
        }

        var searchMode = DetectSearchMode(SearchText);
        if (searchMode != SearchMode.VehicleNumber)
        {
            SetEligibilityState(
                "Search by vehicle number to add a new vehicle",
                "Phone and owner searches are for finding existing vehicles. Use a vehicle number search to create a new vehicle usage record.",
                canAddUsage: false,
                EligibilityDisplayTone.Neutral);
            return;
        }

        SetEligibilityState(
            "New vehicle can be added for this promotion",
            "No existing vehicle matched this vehicle number. Use Add Usage to create the vehicle record and save the promotion usage.",
            canAddUsage: true,
            EligibilityDisplayTone.Positive);
    }

    private void SetEligibilityState(string title, string message, bool canAddUsage, EligibilityDisplayTone tone)
    {
        EligibilityTitle = title;
        EligibilityMessage = message;
        CanAddUsage = canAddUsage;
        IsEligibilityPositive = tone == EligibilityDisplayTone.Positive;
        IsEligibilityWarning = tone == EligibilityDisplayTone.Warning;
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

    private static bool TryParseOptionalFilterDate(string value, out DateTime? parsedDate)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsedDate = null;
            return true;
        }

        if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate) ||
            DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out exactDate))
        {
            parsedDate = exactDate.Date;
            return true;
        }

        parsedDate = null;
        return false;
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

    private enum EligibilityDisplayTone
    {
        Neutral,
        Positive,
        Warning
    }
}
