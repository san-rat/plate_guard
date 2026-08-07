namespace PlateGuard.Cloud;

public sealed class CloudSyncOptions
{
    public const string SupabaseUrlEnvironmentVariableName = "PLATEGUARD_SUPABASE_URL";
    public const string SupabaseAnonKeyEnvironmentVariableName = "PLATEGUARD_SUPABASE_ANON_KEY";
    public const string ServiceAccountEmailEnvironmentVariableName = "PLATEGUARD_SUPABASE_EMAIL";
    public const string ServiceAccountPasswordEnvironmentVariableName = "PLATEGUARD_SUPABASE_PASSWORD";

    public string SupabaseUrl { get; init; } = string.Empty;
    public string SupabaseAnonKey { get; init; } = string.Empty;
    public string ServiceAccountEmail { get; init; } = string.Empty;
    public string ServiceAccountPassword { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SupabaseUrl) &&
        !string.IsNullOrWhiteSpace(SupabaseAnonKey) &&
        !string.IsNullOrWhiteSpace(ServiceAccountEmail) &&
        !string.IsNullOrWhiteSpace(ServiceAccountPassword);

    public static CloudSyncOptions FromEnvironment()
    {
        return new CloudSyncOptions
        {
            SupabaseUrl = Environment.GetEnvironmentVariable(SupabaseUrlEnvironmentVariableName) ?? string.Empty,
            SupabaseAnonKey = Environment.GetEnvironmentVariable(SupabaseAnonKeyEnvironmentVariableName) ?? string.Empty,
            ServiceAccountEmail = Environment.GetEnvironmentVariable(ServiceAccountEmailEnvironmentVariableName) ?? string.Empty,
            ServiceAccountPassword = Environment.GetEnvironmentVariable(ServiceAccountPasswordEnvironmentVariableName) ?? string.Empty
        };
    }
}
