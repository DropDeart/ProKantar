using BaseForge.API.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Tracking;
using Tracking.Data;
using Tracking.Integration.Events;

// h2c (TLS'siz HTTP/2): container ağında gRPC istemci çağrıları için.
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// Yerel 'dotnet run': .env'deki secret'lar (gerçek ortam değişkenleri önceliklidir).
DotNetEnv.Env.NoClobber().Load();

var builder = WebApplication.CreateBuilder(args);
builder.AddBaseForgeLogging("tracking");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(cors => cors.AddPolicy("ConfiguredOrigins", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    }
}));

builder.Services.AddControllers();
builder.Services.AddGrpc(grpc => grpc.Interceptors.Add<BaseForge.API.Grpc.CorrelationIdServerInterceptor>());
builder.Services.AddOpenApi(openApi =>
{
    openApi.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Tracking API";
        document.Info.Version = "v1";
        document.Info.Description = "# Tracking API\n\nŞoför telefonundan konum alır, sevkiyat duraklarına varış/ayrılışı işler (PostGIS).";
        return Task.CompletedTask;
    });
    openApi.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        };
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });
        return Task.CompletedTask;
    });
});

builder.Services.AddBaseForge(options =>
{
    options.UsePostgreSQL<TrackingDbContext>(
        builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default tanımlı değil."));
    options.EnableCQRS(typeof(Program).Assembly);
    options.EnableAuditLog();
    // Yalnızca ICurrentTenant için: entity'ler ITenantEntity değil (şoför token'ında tenant yok),
    // personel sorguları kiracıyı kendisi filtreler.
    options.EnableMultiTenancy();
    options.EnableJwt(jwt =>
    {
        jwt.Authority = builder.Configuration["Auth:Authority"] ?? "http://host.docker.internal:8081";
        jwt.Issuer = builder.Configuration["Auth:Issuer"] ?? string.Empty;
        jwt.Audience = "baseforge-api";
        jwt.RequireHttpsMetadata = false;
        jwt.ServiceClientId = builder.Configuration["Auth:ServiceClient:ClientId"];
        jwt.ServiceClientSecret = builder.Configuration["Auth:ServiceClient:ClientSecret"];
    });
    options.EnableRabbitMq(mq =>
    {
        mq.Host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        mq.Port = int.Parse(builder.Configuration["RabbitMq:Port"] ?? "5672");
        mq.Username = builder.Configuration["RabbitMq:Username"] ?? "guest";
        mq.Password = builder.Configuration["RabbitMq:Password"] ?? "guest";
        mq.Subscribe<ShipmentDispatchedEvent>(ShipmentDispatchedEvent.Type, "tracking.SyncTripOnShipmentDispatched");
        mq.Subscribe<WeighingCompletedEvent>(WeighingCompletedEvent.Type, "tracking.ConfirmWeighbridgeOnWeighingCompleted");
    });
});

builder.Services.AddTracking(builder.Configuration);

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// EF tabloları (Trips, TripStops, outbox/inbox). Konum tablosunu PingPartitionMaintenanceService kurar.
try
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<TrackingDbContext>().Database.EnsureCreated();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Veritabanı şeması oluşturulamadı (Postgres çalışıyor mu?).");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Tracking API");
        options.AddHttpAuthentication("Bearer", auth => auth.WithToken(string.Empty));
        options.AddPreferredSecuritySchemes("Bearer");
        options.EnablePersistentAuthentication();
    });
}

app.UseCors("ConfiguredOrigins");
app.UseBaseForge();
app.MapControllers();
app.MapGrpcService<Tracking.Grpc.TripGrpcService>().RequireAuthorization();
app.Run();
