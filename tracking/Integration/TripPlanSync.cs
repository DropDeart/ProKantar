using Tracking.Domain;
using Tracking.Entities;
using Tracking.Integration.Events;

namespace Tracking.Integration;

/// <summary>Yayınlanan sevkiyat planını sefere uygular.</summary>
internal static class TripPlanSync
{
    /// <summary>Yeni sefer oluşturur.</summary>
    public static Trip Create(ShipmentDispatchedEvent plan, Guid driverUserId)
    {
        var trip = new Trip { Id = plan.ShipmentId, TenantId = plan.TenantId };
        Refresh(trip, plan, driverUserId);
        return trip;
    }

    /// <summary>
    /// Planlı seferi günceller: durak Id'leri kantar'dakiyle aynı olduğundan eşleşen güncellenir,
    /// yenisi eklenir, plandan çıkan Skipped olur (silinmez, iz kalır). Dönen yeni duraklar context'e eklenmeli.
    /// </summary>
    public static IReadOnlyList<TripStop> Refresh(Trip trip, ShipmentDispatchedEvent plan, Guid driverUserId)
    {
        trip.ShipmentNo = plan.ShipmentNo;
        trip.Direction = plan.Direction;
        trip.StationId = plan.StationId;
        trip.VehicleId = plan.VehicleId;
        trip.Plate = plan.Plate;
        trip.DriverId = plan.DriverId;
        trip.DriverUserId = driverUserId;
        trip.DriverName = plan.DriverName;
        trip.PlannedAt = plan.PlannedAt;

        var added = new List<TripStop>();
        foreach (var payload in plan.Stops)
        {
            var stop = trip.Stops.Find(s => s.Id == payload.ShipmentStopId);
            if (stop is null)
            {
                stop = new TripStop { Id = payload.ShipmentStopId, TripId = trip.Id };
                trip.Stops.Add(stop);
                added.Add(stop);
            }

            Apply(stop, payload);
        }

        foreach (var removed in trip.Stops.Where(s => plan.Stops.TrueForAll(p => p.ShipmentStopId != s.Id)))
        {
            removed.Status = StopStatus.Skipped;
        }

        return added;
    }

    private static void Apply(TripStop stop, ShipmentStopPayload payload)
    {
        stop.Sequence = payload.Sequence;
        stop.StopType = payload.StopType;
        stop.Name = payload.Name;
        stop.Location = GeoPoints.Create((double)payload.Latitude, (double)payload.Longitude);
        stop.RadiusM = payload.RadiusM;
        stop.PlannedAt = payload.PlannedAt;
        stop.WaybillId = payload.WaybillId;
        if (stop.Status == StopStatus.Skipped)
        {
            stop.Status = StopStatus.Pending;
        }
    }
}
