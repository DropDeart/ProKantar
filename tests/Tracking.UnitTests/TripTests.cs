using Tracking.Entities;
using static Tracking.UnitTests.TripBuilder;

namespace Tracking.UnitTests;

public class TripTests
{
    [Fact]
    public void StartIfPlanned_SetsActiveOnce()
    {
        var trip = Trip(Stop(41, 29));
        trip.StartIfPlanned(T0);
        trip.StartIfPlanned(T0.AddHours(1));

        Assert.Equal(TripStatus.Active, trip.Status);
        Assert.Equal(T0, trip.StartedAt);
    }

    [Fact]
    public void Complete_RequiresEveryStopDone()
    {
        var departed = Stop(41, 29);
        var pending = Stop(41.1, 29.1, sequence: 2);
        var trip = Trip(departed, pending);
        trip.StartIfPlanned(T0);
        departed.MarkDeparted(T0.AddMinutes(10));

        Assert.False(trip.CompleteIfAllStopsDone(T0.AddMinutes(20)));

        pending.Confirm(T0.AddMinutes(30));
        Assert.True(trip.CompleteIfAllStopsDone(T0.AddMinutes(30)));
        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Equal(T0.AddMinutes(30), trip.EndedAt);
    }

    [Fact]
    public void Complete_CountsSkippedAsDone()
    {
        var skipped = Stop(41, 29);
        skipped.Status = StopStatus.Skipped;
        var confirmed = Stop(41.1, 29.1, sequence: 2);
        confirmed.Confirm(T0);
        var trip = Trip(skipped, confirmed);

        Assert.True(trip.CompleteIfAllStopsDone(T0));
    }

    [Fact]
    public void Complete_WithoutStops_StaysOpen()
        => Assert.False(Trip().CompleteIfAllStopsDone(T0));

    [Fact]
    public void Confirm_KeepsFirstTime()
    {
        var stop = Stop(41, 29);
        stop.Confirm(T0);
        stop.Confirm(T0.AddHours(1));

        Assert.Equal(T0, stop.ConfirmedAt);
    }
}
