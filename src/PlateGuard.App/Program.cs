using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using PlateGuard.App.Composition;
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
        using var serviceProvider = new ServiceCollection()
            .AddPlateGuardApplication()
            .BuildServiceProvider();

        App.ConfigureServices(serviceProvider);

        var databaseInitializer = serviceProvider.GetRequiredService<PlateGuardDatabaseInitializer>();
        databaseInitializer.InitializeAsync().GetAwaiter().GetResult();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
