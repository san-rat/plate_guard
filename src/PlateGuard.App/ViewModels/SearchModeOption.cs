namespace PlateGuard.App.ViewModels;

public sealed class SearchModeOption(SearchMode mode, string label)
{
    public SearchMode Mode { get; } = mode;
    public string Label { get; } = label;
}

public enum SearchMode
{
    Auto,
    VehicleNumber,
    PhoneNumber,
    OwnerName
}
