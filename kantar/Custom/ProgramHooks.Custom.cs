using BaseForge.Infrastructure.Messaging;
using Kantar.Custom.EDespatch;
using Kantar.Custom.Shipments;
using Kantar.Custom.Tracking;
using Kantar.Data;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kantar;

/// <summary>Elle yazılan servis kayıtları (generate dokunmaz).</summary>
internal static partial class ProgramHooks
{
    static partial void ConfigureServices(WebApplicationBuilder builder)
    {
        // Sevkiyat yol ön izlemesi (OSRM); sonuçlar önbellekte.
        builder.Services.AddMemoryCache();
        builder.Services.Configure<Kantar.Custom.Shipments.Routing.RoutingOptions>(
            builder.Configuration.GetSection(Kantar.Custom.Shipments.Routing.RoutingOptions.Section));
        builder.Services.AddHttpClient<Kantar.Custom.Shipments.Routing.OsrmRoutePlanner>((sp, http) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Kantar.Custom.Shipments.Routing.RoutingOptions>>().Value;
            http.BaseAddress = new Uri(options.OsrmUrl.EndsWith('/') ? options.OsrmUrl : options.OsrmUrl + "/");
            http.Timeout = TimeSpan.FromSeconds(10);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ProKantar/1.0");
        });

        // MediatR ön işlemcileri kendiliğinden taramaz; generate edilen komutların kuralları (Custom/ altındaki
        // IRequestPreProcessor'lar: durum kuralları, kantar bağlantısı) burada bağlanır.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>)));
        var processors = typeof(ProgramHooks).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Namespace?.StartsWith("Kantar.Custom", StringComparison.Ordinal) == true)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestPreProcessor<>))
                .Select(i => (Service: i, Implementation: t)));
        foreach (var (service, implementation) in processors)
        {
            builder.Services.AddTransient(service, implementation);
        }

        // e-İrsaliye altyapısı: sağlayıcılar, kiracı ayarı, şifre koruması (Custom/EDespatch).
        builder.AddEDespatch();
    }

    static partial void ConfigureRabbitMq(RabbitMqOptions mq)
    {
        // Sefer ilerleyince/bitince sevkiyat durumu güncellenir (tracking → kantar).
        mq.Subscribe<TripStatusChangedEvent>(TripStatusChangedEvent.Type, "kantar.SyncShipmentOnTripStatusChanged");
    }

    static partial void ConfigureApp(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<KantarDbContext>().Database;

            // EnsureCreated var olan tabloya indeks eklemez; elle eklenen indeksler burada idempotent kurulur.
            try
            {
                database.ExecuteSqlRaw(ShipmentNumbers.UniqueIndexSql);
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Sevkiyat numarası benzersizlik indeksi kurulamadı (çift numara var mı?).");
            }

            // Durak yük satırları tablosu (CREATE TABLE IF NOT EXISTS).
            database.ExecuteSqlRaw(ShipmentLoadSchema.Sql);
        }

        // e-İrsaliye ayar/sayaç/anahtar tabloları (CREATE TABLE IF NOT EXISTS).
        app.EnsureEDespatchSchema();
    }
}
