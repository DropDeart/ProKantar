using Kantar.Custom.EDespatch.Issuing;
using Kantar.Custom.EDespatch.Parties;
using Kantar.Custom.EDespatch.Providers;
using Kantar.Custom.EDespatch.Providers.ELogo;
using Kantar.Custom.EDespatch.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kantar.Custom.EDespatch;

/// <summary>e-İrsaliye modülünün servis kaydı ve açılış şeması.</summary>
internal static class EDespatchRegistration
{
    /// <summary>Modül servislerini ekler.</summary>
    public static void AddEDespatch(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var connectionString = builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default tanımlı değil.");
        services.AddDbContext<EDespatchDbContext>(o => o.UseNpgsql(connectionString));

        // Şifreler için anahtar halkası DB'de: container yenilense de kayıtlı şifreler çözülebilir.
        services.AddDataProtection().SetApplicationName("ProKantar.Kantar");
        services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp => new ConfigureOptions<KeyManagementOptions>(
            o => o.XmlRepository = new EDespatchKeyRepository(sp.GetRequiredService<IServiceScopeFactory>())));
        services.AddSingleton<EDespatchSecretProtector>();
        services.AddScoped<EDespatchSettingsStore>();

        services.AddSingleton<IUblDespatchBuilder, UblDespatchBuilder>();
        services.Configure<ELogoOptions>(builder.Configuration.GetSection(ELogoOptions.Section));
        services.AddHttpClient(ELogoSoapClient.HttpClientName, (sp, http) =>
        {
            http.Timeout = sp.GetRequiredService<IOptions<ELogoOptions>>().Value.Timeout;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(ELogoSoapClient.UserAgent);
        });
        services.AddTransient<ELogoSoapClient>();

        // Yeni entegratör / GİB doğrudan: IEDespatchProvider yazıp buraya bir satır ekleyin.
        services.AddTransient<IEDespatchProvider, PaperOnlyProvider>();
        services.AddTransient<IEDespatchProvider, ManualXmlProvider>();
        services.AddTransient<IEDespatchProvider, ELogoProvider>();
        services.AddTransient<IEDespatchProviderRegistry, EDespatchProviderRegistry>();

        // Tartımdan irsaliye: gönderen künyesi (identity) ve şoför TCKN'si (platform) servis jetonuyla okunur;
        // AddBaseForgeCallCredentials bilinçli olarak yok (kullanıcı jetonunu iletirdi, bu uçlar bf_svc ister).
        services.AddGrpcClient<Identity.Grpc.TenantService.TenantServiceClient>(o => o.Address = new Uri(
                builder.Configuration["Grpc:Identity"] ?? throw new InvalidOperationException("Grpc:Identity tanımlı değil.")))
            .AddInterceptor<BaseForge.API.Grpc.CorrelationIdClientInterceptor>();
        services.AddGrpcClient<Platform.Grpc.DriverService.DriverServiceClient>(o => o.Address = new Uri(
                builder.Configuration["Grpc:Platform"] ?? throw new InvalidOperationException("Grpc:Platform tanımlı değil.")))
            .AddInterceptor<BaseForge.API.Grpc.CorrelationIdClientInterceptor>();
        services.AddScoped<ISupplierDirectory, SupplierDirectory>();
        services.AddScoped<IDriverDirectory, DriverDirectory>();
        services.AddScoped<DespatchDraftBuilder>();
        services.AddScoped<DespatchDelivery>();
    }

    /// <summary>Modül tablolarını ve Waybill ek kolonlarını idempotent kurar (EnsureCreated var olan DB'yi değiştirmez).</summary>
    public static void EnsureEDespatchSchema(this WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<EDespatchDbContext>().Database.ExecuteSqlRaw(EDespatchSchema.Sql);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "e-İrsaliye tabloları kurulamadı (Postgres çalışıyor mu?).");
        }

        try
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<EDespatchDbContext>().Database.ExecuteSqlRaw(WaybillSchema.Sql);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "İrsaliye kolonları/indeksleri kurulamadı (tartım başına çift geçerli irsaliye var mı?).");
        }
    }
}
