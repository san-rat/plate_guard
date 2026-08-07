using System.Text.Json;

namespace PlateGuard.Cloud;

public sealed class CloudSyncOptions
{
    public const string SupabaseUrlEnvironmentVariableName = "PLATEGUARD_SUPABASE_URL";
    public const string SupabaseAnonKeyEnvironmentVariableName = "PLATEGUARD_SUPABASE_ANON_KEY";
    public const string ServiceAccountEmailEnvironmentVariableName = "PLATEGUARD_SUPABASE_EMAIL";
    public const string ServiceAccountPasswordEnvironmentVariableName = "PLATEGUARD_SUPABASE_PASSWORD";
    public const string ConfigurationFileName = "cloudsync.json";
    public const string ApplicationFolderName = "PlateGuard";

    private static readonly JsonSerializerOptions FileSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string SupabaseUrl { get; init; } = string.Empty;
    public string SupabaseAnonKey { get; init; } = string.Empty;
    public string ServiceAccountEmail { get; init; } = string.Empty;
    public string ServiceAccountPassword { get; init; } = string.Empty;

    // Set when a configuration file exists but could not be used. Without this the app would
    // start looking perfectly healthy while silently never syncing, which is the failure mode
    // an installed shop is least equipped to notice.
    public string? ConfigurationError { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SupabaseUrl) &&
        !string.IsNullOrWhiteSpace(SupabaseAnonKey) &&
        !string.IsNullOrWhiteSpace(ServiceAccountEmail) &&
        !string.IsNullOrWhiteSpace(ServiceAccountPassword);

    public static string GetConfigurationFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, ApplicationFolderName, ConfigurationFileName);
    }

    // The installer writes the configuration file so a delivered machine needs no manual setup.
    // Environment variables still win per field, which keeps development and tests able to point
    // at a throwaway project without editing the file an installed shop depends on.
    public static CloudSyncOptions Load()
    {
        return Load(GetConfigurationFilePath());
    }

    public static CloudSyncOptions Load(string configurationFilePath)
    {
        var (file, error) = ReadConfigurationFile(configurationFilePath);

        return new CloudSyncOptions
        {
            SupabaseUrl = Resolve(SupabaseUrlEnvironmentVariableName, file?.SupabaseUrl),
            SupabaseAnonKey = Resolve(SupabaseAnonKeyEnvironmentVariableName, file?.SupabaseAnonKey),
            ServiceAccountEmail = Resolve(ServiceAccountEmailEnvironmentVariableName, file?.ServiceAccountEmail),
            ServiceAccountPassword = Resolve(ServiceAccountPasswordEnvironmentVariableName, file?.ServiceAccountPassword),
            ConfigurationError = error
        };
    }

    private static string Resolve(string environmentVariableName, string? fileValue)
    {
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
        return string.IsNullOrWhiteSpace(environmentValue)
            ? fileValue?.Trim() ?? string.Empty
            : environmentValue.Trim();
    }

    // A missing file is not an error: developers run from environment variables, and an
    // install that deliberately ships without cloud sync simply stays unconfigured.
    private static (CloudSyncConfigurationFile? File, string? Error) ReadConfigurationFile(string configurationFilePath)
    {
        if (!File.Exists(configurationFilePath))
        {
            return (null, null);
        }

        try
        {
            var contents = File.ReadAllText(configurationFilePath);
            var file = JsonSerializer.Deserialize<CloudSyncConfigurationFile>(contents, FileSerializerOptions);
            return file is null
                ? (null, $"{configurationFilePath} is empty.")
                : (file, null);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return (null, $"{configurationFilePath} could not be read: {exception.Message}");
        }
    }

    private sealed class CloudSyncConfigurationFile
    {
        public string? SupabaseUrl { get; set; }
        public string? SupabaseAnonKey { get; set; }
        public string? ServiceAccountEmail { get; set; }
        public string? ServiceAccountPassword { get; set; }
    }
}
