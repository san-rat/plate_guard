using Microsoft.Extensions.DependencyInjection;
using PlateGuard.App.Composition;
using PlateGuard.Data.Db;

namespace PlateGuard.IntegrationTests.Infrastructure;

internal sealed class IntegrationTestApp : IAsyncDisposable
{
    private readonly bool _ownsCleanup;

    private IntegrationTestApp(ServiceProvider serviceProvider, string rootDirectory, string dbPath, bool ownsCleanup)
    {
        ServiceProvider = serviceProvider;
        RootDirectory = rootDirectory;
        DbPath = dbPath;
        _ownsCleanup = ownsCleanup;
    }

    public ServiceProvider ServiceProvider { get; }
    public string RootDirectory { get; }
    public string DbPath { get; }

    public static async Task<IntegrationTestApp> CreateAsync()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "PlateGuard.IntegrationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        var dbPath = Path.Combine(rootDirectory, "plateguard-integration.db");
        return await CreateCoreAsync(rootDirectory, dbPath, deleteExistingDatabase: true, ownsCleanup: true);
    }

    public Task<IntegrationTestApp> CreateRestartedAsync()
    {
        return CreateCoreAsync(RootDirectory, DbPath, deleteExistingDatabase: false, ownsCleanup: false);
    }

    public T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public async ValueTask DisposeAsync()
    {
        await ServiceProvider.DisposeAsync();

        if (_ownsCleanup)
        {
            if (string.Equals(
                    Environment.GetEnvironmentVariable(PlateGuardDatabasePathProvider.DatabasePathEnvironmentVariableName),
                    DbPath,
                    StringComparison.Ordinal))
            {
                Environment.SetEnvironmentVariable(PlateGuardDatabasePathProvider.DatabasePathEnvironmentVariableName, null);
            }

            await DeleteIfExistsAsync(DbPath);
            await DeleteIfExistsAsync($"{DbPath}-shm");
            await DeleteIfExistsAsync($"{DbPath}-wal");

            if (Directory.Exists(RootDirectory))
            {
                try
                {
                    Directory.Delete(RootDirectory, recursive: true);
                }
                catch (IOException)
                {
                    // SQLite may still be releasing file handles. Leave temp files behind instead of failing the test run.
                }
                catch (UnauthorizedAccessException)
                {
                    // Best-effort cleanup only.
                }
            }
        }
    }

    private static async Task<IntegrationTestApp> CreateCoreAsync(
        string rootDirectory,
        string dbPath,
        bool deleteExistingDatabase,
        bool ownsCleanup)
    {
        Directory.CreateDirectory(rootDirectory);

        if (deleteExistingDatabase)
        {
            await DeleteIfExistsAsync(dbPath);
            await DeleteIfExistsAsync($"{dbPath}-shm");
            await DeleteIfExistsAsync($"{dbPath}-wal");
        }

        Environment.SetEnvironmentVariable(PlateGuardDatabasePathProvider.DatabasePathEnvironmentVariableName, dbPath);

        var serviceProvider = new ServiceCollection()
            .AddPlateGuardApplication()
            .BuildServiceProvider();

        var initializer = serviceProvider.GetRequiredService<PlateGuardDatabaseInitializer>();
        await initializer.InitializeAsync();

        return new IntegrationTestApp(serviceProvider, rootDirectory, dbPath, ownsCleanup);
    }

    private static async Task DeleteIfExistsAsync(string path)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException) when (attempt < 4)
            {
                await Task.Delay(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 4)
            {
                await Task.Delay(100);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }
    }
}
