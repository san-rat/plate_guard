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
using PlateGuard.Data.Db;
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
            var dbContextFactory = new PlateGuardDbContextFactory();
            var vehicleRepository = new VehicleRepository(dbContextFactory);
            var promotionRepository = new PromotionRepository(dbContextFactory);
            var promotionUsageRepository = new PromotionUsageRepository(dbContextFactory);
            var promotionUsageTransactionalWriter = new PromotionUsageTransactionalWriter(dbContextFactory);
            var settingsRepository = new SettingsRepository(dbContextFactory);
            IVehicleService vehicleService = new VehicleService(vehicleRepository);
            IPromotionService promotionService = new PromotionService(promotionRepository);
            IPromotionUsageService promotionUsageService = new PromotionUsageService(
                vehicleRepository,
                promotionRepository,
                promotionUsageRepository,
                settingsRepository,
                promotionUsageTransactionalWriter);
            ISettingsService settingsService = new SettingsService(settingsRepository);
            IExportService exportService = new ExportService(settingsRepository);
            var mainWindowViewModel = new MainWindowViewModel(
                vehicleService,
                promotionService,
                promotionUsageService,
                settingsService,
                exportService);

            desktop.MainWindow = new MainWindow(promotionService, promotionUsageService)
            {
                DataContext = mainWindowViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
