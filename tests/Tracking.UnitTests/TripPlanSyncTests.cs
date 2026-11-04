using Tracking.Entities;
using Tracking.Integration;
using Tracking.Integration.Events;

namespace Tracking.UnitTests;

public class TripPlanSyncTests
{
    private static readonly Guid StopA = Guid.NewGuid();
    private static readonly Guid StopB = Guid.NewGuid();
    private static readonly Guid StopC = Guid.NewGuid();

    private static ShipmentDispatchedEvent Plan(params Guid[] stopIds) => new()
    {
        TenantId = Guid.NewGuid(),
        ShipmentId = Guid.NewGuid(),
        ShipmentNo = "SV-1",
        Direction = TripDirection.Outbound,
        Plate = "34ABC123",
        DriverId = Guid.NewGuid(),
        Stops = stopIds.Select((id, i) => new ShipmentStopPayload(id, i + 1, StopType.Delivery, $"D{i + 1}", 41.0m + i, 29.0m, 150, null, null)).ToList(),
    };

    [Fact]
    public void Create_UsesShipmentIdsAndPoint()
    {
        var plan = Plan(StopA);
        var trip = TripPlanSync.Create(plan, Guid.NewGuid());

        Assert.Equal(plan.ShipmentId, trip.Id);
        var stop = Assert.Single(trip.Stops);
        Assert.Equal(StopA, stop.Id);
        Assert.Equal(41.0, stop.Location.Y);
        Assert.Equal(29.0, stop.Location.X);
        Assert.Equal(4326, stop.Location.SRID);
    }

    [Fact]
    public void Refresh_UpdatesMatching_AddsNew_SkipsRemoved()
    {
        var trip = TripPlanSync.Create(Plan(StopA, StopB), Guid.NewGuid());

        var added = TripPlanSync.Refresh(trip, Plan(StopB, StopC), Guid.NewGuid());

        Assert.Equal(StopC, Assert.Single(added).Id);
        Assert.Equal(StopStatus.Skipped, trip.Stops.Single(s => s.Id == StopA).Status);
        Assert.Equal(1, trip.Stops.Single(s => s.Id == StopB).Sequence);
        Assert.Equal(StopStatus.Pending, trip.Stops.Single(s => s.Id == StopC).Status);
    }
}
