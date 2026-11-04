using BaseForge.Core.Interfaces;
using BaseForge.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracking.Data;
using Tracking.Data.Pings;
using Tracking.Domain;
using Tracking.Entities;

namespace Tracking.UnitTests.Integration;

/// <summary>Gerçek PostGIS'te şema, bölümler, konum yazma ve rota SQL'leri.</summary>
public sealed class PostGisStoreTests
{
    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddBaseForgeInfrastructure<TrackingDbContext>(o => o.UseNpgsql(PostGisFactAttribute.ConnectionString));
        services.AddScoped<IPingPartitionManager, PingPartitionManager>();
        services.AddScoped<ILocationPingStore, LocationPingStore>();
        services.AddScoped<ITripRouteStore, TripRouteStore>();
        return services.BuildServiceProvider();
    }

    [PostGisFact]
    public async Task Pings_AreStored_AndRouteIsRecorded()
    {
        await using var provider = CreateServices();
        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var ct = CancellationToken.None;
        var now = DateTimeOffset.UtcNow;

        var db = sp.GetRequiredService<TrackingDbContext>();
        await db.Database.EnsureCreatedAsync(ct);
        var partitions = sp.GetRequiredService<IPingPartitionManager>();
        await partitions.EnsureTableAsync(ct);
        await partitions.EnsurePartitionsAsync(now, monthsAhead: 2, ct);

        var trip = new Trip { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), ShipmentNo = "IT-1", Plate = "34IT1", PlannedAt = now.AddHours(-1) };
        trip.Stops.Add(new TripStop { Id = Guid.NewGuid(), TripId = trip.Id, Sequence = 1, Name = "Şantiye", Location = GeoPoints.Create(41.0, 29.0), RadiusM = 150 });
        db.Trips.Add(trip);
        await db.SaveChangesAsync(ct);

        // ~111 m aralıklı 3 nokta (kuzeye) → rota ≈ 222 m. Bir konumda doğruluk/hız yok (null dizi elemanı).
        var pings = new List<PingSample>
        {
            new(41.000, 29.0, 5, 10, 0, now.AddMinutes(-3)),
            new(41.001, 29.0, null, null, null, now.AddMinutes(-2)),
            new(41.002, 29.0, 8, 12, 0, now.AddMinutes(-1)),
        };
        var store = sp.GetRequiredService<ILocationPingStore>();
        await store.AddAsync(trip.Id, Guid.NewGuid(), pings, ct);

        var line = await store.GetLineGeoJsonAsync(trip.Id, now.AddHours(-1), ct);
        Assert.NotNull(line);
        Assert.Contains("LineString", line, StringComparison.Ordinal);

        var last = await store.GetLastAsync(trip.Id, now.AddHours(-1), ct);
        Assert.NotNull(last);
        Assert.Equal(41.002, last.Latitude, 6);
        Assert.Equal(12, last.SpeedMps);

        var routes = sp.GetRequiredService<ITripRouteStore>();
        await routes.RecordAsync(trip.Id, now.AddHours(-1), ct);
        Assert.Contains("LineString", await routes.GetGeoJsonAsync(trip.Id, ct), StringComparison.Ordinal);

        db.ChangeTracker.Clear();
        var saved = await db.Trips.Include(t => t.Stops).SingleAsync(t => t.Id == trip.Id, ct);
        Assert.InRange(saved.DistanceM!.Value, 215, 230);
        Assert.NotNull(saved.Route);
        Assert.Equal(41.0, saved.Stops[0].Location.Y);
    }

    [PostGisFact]
    public async Task ExpiredPartitions_AreDropped()
    {
        await using var provider = CreateServices();
        await using var scope = provider.CreateAsyncScope();
        var partitions = scope.ServiceProvider.GetRequiredService<IPingPartitionManager>();
        var ct = CancellationToken.None;
        var now = DateTimeOffset.UtcNow;

        await scope.ServiceProvider.GetRequiredService<TrackingDbContext>().Database.EnsureCreatedAsync(ct);
        await partitions.EnsureTableAsync(ct);
        await partitions.EnsurePartitionsAsync(now.AddMonths(-8), monthsAhead: 0, ct);   // 9 ve 8 ay önceki bölümler

        var dropped = await partitions.DropExpiredAsync(now, retentionMonths: 6, ct);

        Assert.Contains(PingPartitions.NameFor(PingPartitions.MonthStart(now.AddMonths(-8))), dropped);
        Assert.DoesNotContain(PingPartitions.NameFor(PingPartitions.MonthStart(now)), dropped);
    }
}
