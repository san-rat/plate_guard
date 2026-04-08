namespace PlateGuard.Core.Models;

public sealed class UpdateAppSettingsResult : OperationResult
{
    public AppSettings? Settings { get; set; }
}
