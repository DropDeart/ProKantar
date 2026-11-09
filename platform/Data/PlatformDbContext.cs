using BaseForge.Core.Interfaces;
using BaseForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Platform.Entities;

namespace Platform.Data;

/// <summary>Platform servisinin EF Core context'i.</summary>
public sealed class PlatformDbContext : BaseForgeDbContext
{
    /// <summary>Yeni bir PlatformDbContext oluşturur.</summary>
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options, ICurrentUser? currentUser = null, ICurrentTenant? currentTenant = null)
        : base(options, currentUser, currentTenant)
    {
    }

    /// <summary>Carrier tablosu.</summary>
    public DbSet<Carrier> Carriers => Set<Carrier>();
    /// <summary>Driver tablosu.</summary>
    public DbSet<Driver> Drivers => Set<Driver>();
    /// <summary>Vehicle tablosu.</summary>
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    /// <summary>Trailer tablosu.</summary>
    public DbSet<Trailer> Trailers => Set<Trailer>();
    /// <summary>DriverVehicle tablosu.</summary>
    public DbSet<DriverVehicle> DriverVehicles => Set<DriverVehicle>();
    /// <summary>Device tablosu.</summary>
    public DbSet<Device> Devices => Set<Device>();
    /// <summary>VehicleModel tablosu (elle eklendi: araç kataloğu, Custom/Fleet).</summary>
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    /// <summary>VehicleSpec tablosu (elle eklendi: araç modeli/ruhsat/filo bağı, Custom/Fleet).</summary>
    public DbSet<VehicleSpec> VehicleSpecs => Set<VehicleSpec>();
    /// <summary>TenantDriver tablosu (elle eklendi: firma şoförleri/davetleri, Custom/Drivers).</summary>
    public DbSet<TenantDriver> TenantDrivers => Set<TenantDriver>();

    /// <summary>Şoför bildirimleri (elle eklendi: irsaliye bildirim zinciri, Custom/Notifications).</summary>
    public DbSet<DriverNotification> DriverNotifications => Set<DriverNotification>();

    /// <summary>Şoför bildirimlerinin kanal denemeleri (elle eklendi, Custom/Notifications).</summary>
    public DbSet<DriverNotificationAttempt> DriverNotificationAttempts => Set<DriverNotificationAttempt>();

    /// <summary>Firma bildirimleri (elle eklendi: zil, Custom/Notifications).</summary>
    public DbSet<TenantNotification> TenantNotifications => Set<TenantNotification>();

    /// <summary>Kullanıcının bildirimlere son bakışı (elle eklendi, Custom/Notifications).</summary>
    public DbSet<TenantNotificationCursor> TenantNotificationCursors => Set<TenantNotificationCursor>();

    /// <summary>
    /// Enum alanlar veritabanında sayı değil, değer adıyla (string) saklanır — enum'a değer eklenip sırası
    /// değişse de var olan kayıtlar bozulmaz ve DB okunabilir kalır.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DevicePlatform>().HaveConversion<string>();
        configurationBuilder.Properties<VehicleBodyType>().HaveConversion<string>();
        configurationBuilder.Properties<TenantDriverStatus>().HaveConversion<string>();
        configurationBuilder.Properties<TenantDriverSource>().HaveConversion<string>();
        configurationBuilder.Properties<DriverNotificationStatus>().HaveConversion<string>();
        configurationBuilder.Properties<DriverNotificationChannel>().HaveConversion<string>();
        configurationBuilder.Properties<DriverNotificationResult>().HaveConversion<string>();
        configurationBuilder.Properties<TenantNotificationKind>().HaveConversion<string>();
    }
}
