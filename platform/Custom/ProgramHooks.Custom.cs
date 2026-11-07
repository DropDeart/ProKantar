using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Platform.Custom.Drivers;
using Platform.Custom.Fleet;
using Platform.Custom.Messaging;
using Platform.Data;

namespace Platform;

/// <summary>Elle yazılan servis kayıtları (generate dokunmaz).</summary>
internal static partial class ProgramHooks
{
    static partial void ConfigureServices(WebApplicationBuilder builder)
    {
        // MediatR ön işlemcileri kendiliğinden taramaz; generate edilen komutların kuralları (Custom/ altındaki
        // IRequestPreProcessor'lar, ör. TCKN doğrulaması) burada bağlanır.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>)));
        var processors = typeof(ProgramHooks).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Namespace?.StartsWith("Platform.Custom", StringComparison.Ordinal) == true)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestPreProcessor<>))
                .Select(i => (Service: i, Implementation: t)));
        foreach (var (service, implementation) in processors)
        {
            builder.Services.AddTransient(service, implementation);
        }

        // SMS: Netgsm hesabı .env'de tanımlıysa toplu SMS, değilse log. Şoför davetleri SMS ile bildirilir.
        var netgsm = builder.Configuration.GetSection("Netgsm");
        builder.Services.Configure<NetgsmOptions>(netgsm);
        if (netgsm.Get<NetgsmOptions>()?.IsConfigured == true)
        {
            builder.Services.AddHttpClient<ISmsGateway, NetgsmSmsGateway>(c =>
            {
                c.BaseAddress = new Uri("https://api.netgsm.com.tr/");
                c.Timeout = TimeSpan.FromSeconds(15);
            });
        }
        else
        {
            builder.Services.AddSingleton<ISmsGateway, LogSmsGateway>();
        }

        builder.Services.AddScoped<IDriverNotifier, SmsDriverNotifier>();
    }

    static partial void ConfigureApp(WebApplication app)
    {
        // Araç kataloğu, araç özelliği ve firma şoförleri tabloları (CREATE TABLE IF NOT EXISTS) ve başlangıç kataloğu.
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        db.Database.ExecuteSqlRaw(FleetSchema.Sql);
        db.Database.ExecuteSqlRaw(TenantDriverSchema.Sql);
        VehicleModelCatalog.SeedAsync(db).GetAwaiter().GetResult();
    }
}
