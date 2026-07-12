using System.Collections.Generic;
using PlateGuard.Core.Models;

namespace PlateGuard.App.ViewModels;

public sealed class OwnerSuggestion
{
    public string OwnerName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public IReadOnlyList<Vehicle> Vehicles { get; init; } = [];
}
