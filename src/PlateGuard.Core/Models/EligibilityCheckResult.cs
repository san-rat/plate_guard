namespace PlateGuard.Core.Models;

public sealed class EligibilityCheckResult
{
    public bool IsEligible { get; set; }
    public string Message { get; set; } = string.Empty;
    public Vehicle? Vehicle { get; set; }
    public Promotion? Promotion { get; set; }
    public PromotionUsage? ExistingUsage { get; set; }
}
