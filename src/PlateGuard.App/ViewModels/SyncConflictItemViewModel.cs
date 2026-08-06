using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlateGuard.Cloud;

namespace PlateGuard.App.ViewModels;

public sealed partial class SyncConflictItemViewModel : ObservableObject
{
    private readonly Func<int, Task> _acknowledgeAsync;

    public SyncConflictItemViewModel(SyncConflictItem conflict, Func<int, Task> acknowledgeAsync)
    {
        Id = conflict.Id;
        Plate = conflict.VehicleNumberRaw ?? "Unknown vehicle";
        PromotionName = conflict.PromotionName ?? "Unknown promotion";
        ServiceDate = conflict.ServiceDate;
        ServiceDateText = conflict.ServiceDate?.ToString("d") ?? "Unknown date";
        Kind = conflict.Kind;
        Details = conflict.Details;
        _acknowledgeAsync = acknowledgeAsync;
    }

    public int Id { get; }
    public string Plate { get; }
    public string PromotionName { get; }
    public DateTime? ServiceDate { get; }
    public string ServiceDateText { get; }
    public string Kind { get; }
    public string Details { get; }

    [RelayCommand]
    private Task AcknowledgeAsync()
    {
        return _acknowledgeAsync(Id);
    }
}
