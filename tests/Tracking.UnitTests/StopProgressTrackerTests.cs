using Microsoft.Extensions.Options;
using Tracking.Configuration;
using Tracking.Domain;
using Tracking.Entities;
using static Tracking.UnitTests.TripBuilder;

namespace Tracking.UnitTests;

public class StopProgressTrackerTests
{
    // Durak: 41.0000, 29.0000. Enlemde 0.001° ≈ 111 m.
    private const double Lat = 41.0;
    private const double Lon = 29.0;

    private readonly StopProgressTracker _tracker = new(Options.Create(new TrackingOptions()));

    [Fact]
    public void Ping_InsideRadius_MarksArrived()
    {
        var stop = Stop(Lat, Lon, radiusM: 150);
        _tracker.Apply(Trip(stop), Ping(Lat + 0.001, Lon, minute: 5));

        Assert.Equal(StopStatus.Arrived, stop.Status);
        Assert.Equal(T0.AddMinutes(5), stop.ArrivedAt);
    }

    [Fact]
    public void Ping_OutsideRadius_StaysPending()
    {
        var stop = Stop(Lat, Lon, radiusM: 150);
        _tracker.Apply(Trip(stop), Ping(Lat + 0.002, Lon, minute: 5));

        Assert.Equal(StopStatus.Pending, stop.Status);
    }

    [Fact]
    public void Arrived_JustOutsideRadius_DoesNotDepart()
    {
        // 150 m yarıçap × 1.25 = 187 m; 166 m'de hâlâ "durakta" sayılır (titreme önlenir).
        var stop = Stop(Lat, Lon, radiusM: 150);
        var trip = Trip(stop);
        _tracker.Apply(trip, Ping(Lat, Lon, minute: 1));
        _tracker.Apply(trip, Ping(Lat + 0.0015, Lon, minute: 2));

        Assert.Equal(StopStatus.Arrived, stop.Status);
    }

    [Fact]
    public void Arrived_BeyondDepartureRadius_MarksDeparted()
    {
        var stop = Stop(Lat, Lon, radiusM: 150);
        var trip = Trip(stop);
        _tracker.Apply(trip, Ping(Lat, Lon, minute: 1));
        _tracker.Apply(trip, Ping(Lat + 0.003, Lon, minute: 9));

        Assert.Equal(StopStatus.Departed, stop.Status);
        Assert.Equal(T0.AddMinutes(9), stop.DepartedAt);
    }

    [Fact]
    public void InaccuratePing_IsIgnored()
    {
        var stop = Stop(Lat, Lon, radiusM: 150);
        _tracker.Apply(Trip(stop), Ping(Lat, Lon, minute: 1, accuracyM: 500));

        Assert.Equal(StopStatus.Pending, stop.Status);
    }

    [Fact]
    public void SkippedStop_IsNotChanged()
    {
        var stop = Stop(Lat, Lon);
        stop.Status = StopStatus.Skipped;
        _tracker.Apply(Trip(stop), Ping(Lat, Lon, minute: 1));

        Assert.Equal(StopStatus.Skipped, stop.Status);
    }
}
