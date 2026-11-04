using System.Linq.Expressions;
using Tracking.Data.Pings;
using Tracking.Entities;

namespace Tracking.Features.Trips;

/// <summary>Sefer ve durakları.</summary>
public sealed record TripDto(
    Guid Id,
    string ShipmentNo,
    TripDirection Direction,
    TripStatus Status,
    Guid StationId,
    Guid VehicleId,
    string Plate,
    Guid DriverId,
    string? DriverName,
    DateTimeOffset PlannedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    double? DistanceM,
    IReadOnlyList<TripStopDto> Stops)
{
    /// <summary>Entity'den DTO.</summary>
    public static TripDto From(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);
        return new(trip.Id, trip.ShipmentNo, trip.Direction, trip.Status, trip.StationId, trip.VehicleId,
            trip.Plate, trip.DriverId, trip.DriverName, trip.PlannedAt, trip.StartedAt, trip.EndedAt, trip.DistanceM,
            trip.Stops.OrderBy(s => s.Sequence).Select(TripStopDto.From).ToList());
    }
}

/// <summary>Durak.</summary>
public sealed record TripStopDto(
    Guid Id,
    int Sequence,
    StopType StopType,
    string Name,
    double Latitude,
    double Longitude,
    int RadiusM,
    DateTimeOffset? PlannedAt,
    Guid? WaybillId,
    StopStatus Status,
    DateTimeOffset? ArrivedAt,
    DateTimeOffset? DepartedAt,
    DateTimeOffset? ConfirmedAt)
{
    /// <summary>Entity'den DTO.</summary>
    public static TripStopDto From(TripStop stop)
    {
        ArgumentNullException.ThrowIfNull(stop);
        return new(stop.Id, stop.Sequence, stop.StopType, stop.Name, stop.Location.Y, stop.Location.X, stop.RadiusM,
            stop.PlannedAt, stop.WaybillId, stop.Status, stop.ArrivedAt, stop.DepartedAt, stop.ConfirmedAt);
    }
}

/// <summary>Liste satırı (durak yok).</summary>
public sealed record TripListItemDto(
    Guid Id,
    string ShipmentNo,
    TripDirection Direction,
    TripStatus Status,
    string Plate,
    string? DriverName,
    DateTimeOffset PlannedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt)
{
    /// <summary>EF projeksiyonu.</summary>
    public static readonly Expression<Func<Trip, TripListItemDto>> Projection = t =>
        new TripListItemDto(t.Id, t.ShipmentNo, t.Direction, t.Status, t.Plate, t.DriverName, t.PlannedAt, t.StartedAt, t.EndedAt);
}

/// <summary>Haritada izlenen yol ve son konum.</summary>
/// <param name="RouteGeoJson">GeoJSON LineString; yeterli konum yoksa null.</param>
/// <param name="LastLocation">Bilinen son konum.</param>
public sealed record TripTrackDto(string? RouteGeoJson, LastLocation? LastLocation);
