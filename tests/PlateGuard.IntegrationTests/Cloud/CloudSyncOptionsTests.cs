using PlateGuard.Cloud;

namespace PlateGuard.IntegrationTests.Cloud;

public sealed class CloudSyncOptionsTests
{
    [Fact]
    public void Load_ReadsEveryValueFromTheConfigurationFile()
    {
        using var configurationFile = TemporaryConfigurationFile.Create("""
            {
              "supabaseUrl": "https://example-project.supabase.co",
              "supabaseAnonKey": "fake-anon-key",
              "serviceAccountEmail": "shop@example.invalid",
              "serviceAccountPassword": "fake-password"
            }
            """);

        var options = CloudSyncOptions.Load(configurationFile.Path);

        Assert.True(options.IsConfigured);
        Assert.Null(options.ConfigurationError);
        Assert.Equal("https://example-project.supabase.co", options.SupabaseUrl);
        Assert.Equal("fake-anon-key", options.SupabaseAnonKey);
        Assert.Equal("shop@example.invalid", options.ServiceAccountEmail);
        Assert.Equal("fake-password", options.ServiceAccountPassword);
    }

    [Fact]
    public void Load_PrefersEnvironmentVariablesOverTheConfigurationFile()
    {
        using var configurationFile = TemporaryConfigurationFile.Create("""
            {
              "supabaseUrl": "https://installed-project.supabase.co",
              "supabaseAnonKey": "installed-anon-key",
              "serviceAccountEmail": "shop@example.invalid",
              "serviceAccountPassword": "installed-password"
            }
            """);
        using var environmentOverride = EnvironmentVariableOverride.Set(
            CloudSyncOptions.SupabaseUrlEnvironmentVariableName,
            "https://throwaway-project.supabase.co");

        var options = CloudSyncOptions.Load(configurationFile.Path);

        // Only the overridden field changes; the rest still come from the installed file.
        Assert.Equal("https://throwaway-project.supabase.co", options.SupabaseUrl);
        Assert.Equal("installed-anon-key", options.SupabaseAnonKey);
    }

    [Fact]
    public void Load_WithoutAConfigurationFile_IsUnconfiguredAndReportsNoError()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"plateguard-missing-{Guid.NewGuid():N}.json");

        var options = CloudSyncOptions.Load(missingPath);

        Assert.False(options.IsConfigured);
        Assert.Null(options.ConfigurationError);
    }

    [Fact]
    public void Load_WithAMalformedConfigurationFile_ReportsTheProblemInsteadOfFailingSilently()
    {
        using var configurationFile = TemporaryConfigurationFile.Create("{ this is not json");

        var options = CloudSyncOptions.Load(configurationFile.Path);

        Assert.False(options.IsConfigured);
        Assert.NotNull(options.ConfigurationError);
        Assert.Contains(configurationFile.Path, options.ConfigurationError);
    }

    private sealed class TemporaryConfigurationFile : IDisposable
    {
        private TemporaryConfigurationFile(string path) => Path = path;

        public string Path { get; }

        public static TemporaryConfigurationFile Create(string contents)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"plateguard-cloudsync-{Guid.NewGuid():N}.json");
            File.WriteAllText(path, contents);
            return new TemporaryConfigurationFile(path);
        }

        public void Dispose() => File.Delete(Path);
    }

    private sealed class EnvironmentVariableOverride : IDisposable
    {
        private readonly string _name;
        private readonly string? _originalValue;

        private EnvironmentVariableOverride(string name, string? originalValue)
        {
            _name = name;
            _originalValue = originalValue;
        }

        public static EnvironmentVariableOverride Set(string name, string value)
        {
            var originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
            return new EnvironmentVariableOverride(name, originalValue);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(_name, _originalValue);
    }
}
