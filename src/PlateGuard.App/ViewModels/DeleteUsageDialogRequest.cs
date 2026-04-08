using PlateGuard.Core.Models;

namespace PlateGuard.App.ViewModels;

public sealed class DeleteUsageDialogRequest
{
    public PromotionUsageRecord Record { get; init; } = new();
}
