using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using PlateGuard.App.Composition;
using PlateGuard.Cloud;
using PlateGuard.Data.Db;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace PlateGuard.App;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // One process per database. Two instances on the same SQLite file race each other, and
        // the six-hourly sync makes that worse: both would push and pull the same rows.
        using var singleInstance = new Mutex(true, SingleInstanceMutexName, out var isOnlyInstance);
        if (!isOnlyInstance)
        {
            return;
        }

        // Disposed explicitly instead of with `using`: the container holds SyncScheduler, which
        // implements only IAsyncDisposable, and synchronous Dispose() throws on such a container.
        // Main stays synchronous so Avalonia starts on the STA thread [STAThread] guarantees.
        var serviceProvider = new ServiceCollection()
            .AddPlateGuardApplication()
            .BuildServiceProvider();

        try
        {
            App.ConfigureServices(serviceProvider);

            var databaseInitializer = serviceProvider.GetRequiredService<PlateGuardDatabaseInitializer>();
            databaseInitializer.InitializeAsync().GetAwaiter().GetResult();

            var syncScheduler = serviceProvider.GetRequiredService<SyncScheduler>();
            syncScheduler.Start();

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                syncScheduler.StopAsync().GetAwaiter().GetResult();
            }
        }
        finally
        {
            serviceProvider.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    // Keyed on the resolved database path, not the app, so a second instance pointed at a
    // different PLATEGUARD_DB_PATH still starts. Hashed because a mutex name cannot contain '\'.
    private static string SingleInstanceMutexName
    {
        get
        {
            var path = PlateGuardDatabasePathProvider.GetDatabasePath().ToUpperInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(path));
            return "Local\\PlateGuard-" + Convert.ToHexString(hash, 0, 8);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
