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

    /// <summary>
    /// Enum alanlar veritabanında sayı değil, değer adıyla (string) saklanır — enum'a değer eklenip sırası
    /// değişse de var olan kayıtlar bozulmaz ve DB okunabilir kalır.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DevicePlatform>().HaveConversion<string>();
    }
}
