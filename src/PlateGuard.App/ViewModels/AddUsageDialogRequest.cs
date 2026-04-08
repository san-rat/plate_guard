using System;
using System.Collections.Generic;
using PlateGuard.Core.Models;

namespace PlateGuard.App.ViewModels;

public sealed class AddUsageDialogRequest
{
    public Vehicle? Vehicle { get; init; }
    public Promotion? SelectedPromotion { get; init; }
    public IReadOnlyList<Promotion> AvailablePromotions { get; init; } = Array.Empty<Promotion>();
    public string PrefilledVehicleNumber { get; init; } = string.Empty;
}
