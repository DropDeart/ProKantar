using Tracking.Configuration;
using Tracking.Data.Pings;
using Tracking.Domain;
using Tracking.Features.DriverApp;
using Tracking.Integration;

namespace Tracking;

/// <summary>Tracking'e özgü servis kayıtları (BaseForge kayıtları Program.cs'te).</summary>
internal static class TrackingServiceCollectionExtensions
{
    /// <summary>Kurallar, PostGIS konum deposu, domain servisleri ve platform istemcisi.</summary>
    public static IServiceCollection AddTracking(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TrackingOptions>(configuration.GetSection(TrackingOptions.Section));
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IPingPartitionManager, PingPartitionManager>();
        services.AddScoped<ILocationPingStore, LocationPingStore>();
        services.AddScoped<ITripRouteStore, TripRouteStore>();
        services.AddHostedService<PingPartitionMaintenanceService>();

        services.AddSingleton<IStopProgressTracker, StopProgressTracker>();
        services.AddScoped<ITripCloser, TripCloser>();
        services.AddScoped<IDriverTripFinder, DriverTripFinder>();

        // platform/Driver: kullanıcı isteğinde token iletilir, olay işlerken servis token'ı kullanılır.
        services.AddGrpcClient<Platform.Grpc.DriverService.DriverServiceClient>(o =>
                o.Address = new Uri(configuration["Grpc:Platform"]
                    ?? throw new InvalidOperationException("Grpc:Platform tanımlı değil.")))
            .AddInterceptor<BaseForge.API.Grpc.CorrelationIdClientInterceptor>()
            .AddBaseForgeCallCredentials();
        services.AddScoped<IDriverClient, DriverClient>();

        return services;
    }
}
