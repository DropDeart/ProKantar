using BaseForge.Core.Interfaces;
using BaseForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Tracking.Entities;

namespace Tracking.Data;

/// <summary>Tracking servisinin EF Core context'i (PostGIS).</summary>
public sealed class TrackingDbContext : BaseForgeDbContext
{
    /// <summary>Yeni bir context oluşturur.</summary>
    public TrackingDbContext(DbContextOptions<TrackingDbContext> options, ICurrentUser? currentUser = null, ICurrentTenant? currentTenant = null)
        : base(options, currentUser, currentTenant)
    {
    }

    /// <summary>Seferler.</summary>
    public DbSet<Trip> Trips => Set<Trip>();

    /// <summary>Sefer durakları.</summary>
    public DbSet<TripStop> TripStops => Set<TripStop>();

    /// <summary>BaseForge'un kurduğu Npgsql bağlantısına PostGIS (NetTopologySuite) desteği ekler.</summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.UseNpgsql(npgsql => npgsql.UseNetTopologySuite());
    }

    /// <summary>Enum'lar adıyla saklanır.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<TripDirection>().HaveConversion<string>();
        configurationBuilder.Properties<TripStatus>().HaveConversion<string>();
        configurationBuilder.Properties<StopType>().HaveConversion<string>();
        configurationBuilder.Properties<StopStatus>().HaveConversion<string>();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("postgis");

        // Id'ler kantar'dan gelir (Shipment/ShipmentStop), üretilmez.
        modelBuilder.Entity<Trip>(trip =>
        {
            trip.Property(t => t.Id).ValueGeneratedNever();
            trip.Property(t => t.Route).HasColumnType("geometry(LineString,4326)");
            trip.HasMany(t => t.Stops).WithOne().HasForeignKey(s => s.TripId);
            trip.HasIndex(t => new { t.DriverUserId, t.Status });
            trip.HasIndex(t => new { t.TenantId, t.PlannedAt });
        });

        modelBuilder.Entity<TripStop>(stop =>
        {
            stop.Property(s => s.Id).ValueGeneratedNever();
            stop.Property(s => s.Location).HasColumnType("geometry(Point,4326)");
            stop.HasIndex(s => new { s.TripId, s.Sequence });
        });
    }
}
