using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using PlateGuard.App.Composition;
using PlateGuard.Cloud;
using PlateGuard.Data.Db;
using System;

namespace PlateGuard.App;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
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

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
