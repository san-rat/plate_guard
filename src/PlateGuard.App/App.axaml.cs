using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using PlateGuard.App.ViewModels;
using PlateGuard.App.Views;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Services;
using PlateGuard.Data.Repositories;

namespace PlateGuard.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vehicleRepository = new VehicleRepository();
            var promotionRepository = new PromotionRepository();
            var promotionUsageRepository = new PromotionUsageRepository();
            var settingsRepository = new SettingsRepository();
            IVehicleService vehicleService = new VehicleService(vehicleRepository);
            IPromotionService promotionService = new PromotionService(promotionRepository);
            IPromotionUsageService promotionUsageService = new PromotionUsageService(
                vehicleRepository,
                promotionRepository,
                promotionUsageRepository,
                settingsRepository);
            var mainWindowViewModel = new MainWindowViewModel(
                vehicleService,
                promotionService,
                promotionUsageService);

            desktop.MainWindow = new MainWindow(promotionUsageService)
            {
                DataContext = mainWindowViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
