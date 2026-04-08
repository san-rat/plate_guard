namespace PlateGuard.Core.Models;

public sealed class ChangeDeletePasswordRequest
{
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
}
