using BaseForge.Infrastructure.Messaging;
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
    }

    static partial void ConfigureRabbitMq(RabbitMqOptions mq)
    {
        // Sefer ilerleyince/bitince sevkiyat durumu güncellenir (tracking → kantar).
        mq.Subscribe<TripStatusChangedEvent>(TripStatusChangedEvent.Type, "kantar.SyncShipmentOnTripStatusChanged");
    }

    static partial void ConfigureApp(WebApplication app)
    {
        // EnsureCreated var olan tabloya indeks eklemez; elle eklenen indeksler burada idempotent kurulur.
        try
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<KantarDbContext>().Database.ExecuteSqlRaw(ShipmentNumbers.UniqueIndexSql);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Sevkiyat numarası benzersizlik indeksi kurulamadı (çift numara var mı?).");
        }
    }
}
