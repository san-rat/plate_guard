using PlateGuard.Core.Models;

namespace PlateGuard.App.ViewModels;

public sealed class EditUsageDialogRequest
{
    public PromotionUsageRecord Record { get; init; } = new();
}
