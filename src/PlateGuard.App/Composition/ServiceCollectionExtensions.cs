using Microsoft.Extensions.DependencyInjection;
using PlateGuard.App.ViewModels;
using PlateGuard.App.Views;
using PlateGuard.Core.Interfaces;
using PlateGuard.Core.Services;
using PlateGuard.Data.Db;
using PlateGuard.Data.Repositories;

namespace PlateGuard.App.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlateGuardApplication(this IServiceCollection services)
    {
        services.AddSingleton<PlateGuardDbContextFactory>();
        services.AddSingleton<PlateGuardDatabaseInitializer>();

        services.AddSingleton<IVehicleRepository, VehicleRepository>();
        services.AddSingleton<IPromotionRepository, PromotionRepository>();
        services.AddSingleton<IPromotionUsageRepository, PromotionUsageRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<IPromotionUsageTransactionalWriter, PromotionUsageTransactionalWriter>();

        services.AddSingleton<IVehicleService, VehicleService>();
        services.AddSingleton<IPromotionService, PromotionService>();
        services.AddSingleton<IPromotionUsageService, PromotionUsageService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IExportService, ExportService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}
