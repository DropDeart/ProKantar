using BaseForge.Core.Interfaces;
using BaseForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Kantar.Entities;

namespace Kantar.Data;

/// <summary>Kantar servisinin EF Core context'i.</summary>
public sealed class KantarDbContext : BaseForgeDbContext
{
    /// <summary>Yeni bir KantarDbContext oluşturur.</summary>
    public KantarDbContext(DbContextOptions<KantarDbContext> options, ICurrentUser? currentUser = null, ICurrentTenant? currentTenant = null)
        : base(options, currentUser, currentTenant)
    {
    }

    /// <summary>Station tablosu.</summary>
    public DbSet<Station> Stations => Set<Station>();
    /// <summary>Weighbridge tablosu.</summary>
    public DbSet<Weighbridge> Weighbridges => Set<Weighbridge>();
    /// <summary>Machine tablosu.</summary>
    public DbSet<Machine> Machines => Set<Machine>();
    /// <summary>Company tablosu.</summary>
    public DbSet<Company> Companies => Set<Company>();
    /// <summary>DeliveryLocation tablosu.</summary>
    public DbSet<DeliveryLocation> DeliveryLocations => Set<DeliveryLocation>();
    /// <summary>ErpCompanyLink tablosu.</summary>
    public DbSet<ErpCompanyLink> ErpCompanyLinks => Set<ErpCompanyLink>();
    /// <summary>VehicleType tablosu.</summary>
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    /// <summary>Material tablosu.</summary>
    public DbSet<Material> Materials => Set<Material>();
    /// <summary>TenantVehicle tablosu.</summary>
    public DbSet<TenantVehicle> TenantVehicles => Set<TenantVehicle>();
    /// <summary>Weighing tablosu.</summary>
    public DbSet<Weighing> Weighings => Set<Weighing>();
    /// <summary>WeighingPhoto tablosu.</summary>
    public DbSet<WeighingPhoto> WeighingPhotos => Set<WeighingPhoto>();
    /// <summary>WeighingAuditLog tablosu.</summary>
    public DbSet<WeighingAuditLog> WeighingAuditLogs => Set<WeighingAuditLog>();
    /// <summary>Waybill tablosu.</summary>
    public DbSet<Waybill> Waybills => Set<Waybill>();
    /// <summary>Shipment tablosu.</summary>
    public DbSet<Shipment> Shipments => Set<Shipment>();
    /// <summary>ShipmentStop tablosu.</summary>
    public DbSet<ShipmentStop> ShipmentStops => Set<ShipmentStop>();

    /// <summary>
    /// Enum alanlar veritabanında sayı değil, değer adıyla (string) saklanır — enum'a değer eklenip sırası
    /// değişse de var olan kayıtlar bozulmaz ve DB okunabilir kalır.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<WeighbridgeConnectionType>().HaveConversion<string>();
        configurationBuilder.Properties<MaterialUnit>().HaveConversion<string>();
        configurationBuilder.Properties<WeighingDirection>().HaveConversion<string>();
        configurationBuilder.Properties<WeighingStatus>().HaveConversion<string>();
        configurationBuilder.Properties<WeighingTareSource>().HaveConversion<string>();
        configurationBuilder.Properties<WeighingPhotoStage>().HaveConversion<string>();
        configurationBuilder.Properties<WeighingAuditLogAction>().HaveConversion<string>();
        configurationBuilder.Properties<WaybillWaybillType>().HaveConversion<string>();
        configurationBuilder.Properties<WaybillEDespatchStatus>().HaveConversion<string>();
        configurationBuilder.Properties<WaybillEDespatchProviderKind>().HaveConversion<string>();
        configurationBuilder.Properties<WaybillErpSyncStatus>().HaveConversion<string>();
        configurationBuilder.Properties<ShipmentDirection>().HaveConversion<string>();
        configurationBuilder.Properties<ShipmentStatus>().HaveConversion<string>();
        configurationBuilder.Properties<ShipmentStopStopType>().HaveConversion<string>();
    }
}
