using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;
using System;
using Avalonia.Markup.Xaml;
using PlateGuard.App.Views;

namespace PlateGuard.App;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        Services = serviceProvider;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var serviceProvider = Services ?? throw new InvalidOperationException("Application services have not been configured.");
            desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
