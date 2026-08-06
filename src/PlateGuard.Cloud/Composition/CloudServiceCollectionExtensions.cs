using Microsoft.Extensions.DependencyInjection;

namespace PlateGuard.Cloud.Composition;

public static class CloudServiceCollectionExtensions
{
    public static IServiceCollection AddPlateGuardCloud(this IServiceCollection services)
    {
        services.AddSingleton(CloudSyncOptions.FromEnvironment());
        services.AddSingleton<ISyncLog, TraceSyncLog>();
        services.AddSingleton<ICloudSyncClient, SupabaseCloudSyncClient>();
        services.AddSingleton<SyncReconciler>();
        services.AddSingleton<ISyncEngine, SyncEngine>();
        services.AddSingleton<SyncScheduler>();

        return services;
    }
}
