using Tracking.Domain;
using Tracking.Entities;

namespace Tracking.UnitTests;

/// <summary>Testler için kısa sefer/durak kurucusu.</summary>
internal static class TripBuilder
{
    public static readonly DateTimeOffset T0 = new(2026, 11, 4, 8, 0, 0, TimeSpan.Zero);

    public static Trip Trip(params TripStop[] stops)
    {
        var trip = new Trip { Id = Guid.NewGuid(), PlannedAt = T0 };
        trip.Stops.AddRange(stops);
        return trip;
    }

    public static TripStop Stop(double latitude, double longitude, int radiusM = 150, StopType type = StopType.Delivery, int sequence = 1)
        => new()
        {
            Id = Guid.NewGuid(),
            Sequence = sequence,
            StopType = type,
            Location = GeoPoints.Create(latitude, longitude),
            RadiusM = radiusM,
        };

    public static PingSample Ping(double latitude, double longitude, int minute, double? accuracyM = 10)
        => new(latitude, longitude, accuracyM, null, null, T0.AddMinutes(minute));
}
