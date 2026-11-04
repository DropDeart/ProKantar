using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracking.Data;
using Tracking.Entities;
using Tracking.Integration.Events;

namespace Tracking.Integration.Handlers;

/// <summary>Sevkiyat yayınlanınca seferi açar; planlı seferde planı günceller.</summary>
internal sealed class SyncTripOnShipmentDispatched(
    TrackingDbContext db,
    IDriverClient driverClient,
    IUnitOfWork unitOfWork,
    ILogger<SyncTripOnShipmentDispatched> logger) : INotificationHandler<ShipmentDispatchedEvent>
{
    public async Task Handle(ShipmentDispatchedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var trip = await db.Trips
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t => t.Id == notification.ShipmentId, cancellationToken);

        if (trip is { Status: not TripStatus.Planned })
        {
            logger.LogInformation("Sefer {TripId} {Status}; yeniden yayınlanan plan uygulanmadı.", trip.Id, trip.Status);
            return;
        }

        var driverUserId = trip is not null && trip.DriverId == notification.DriverId
            ? trip.DriverUserId
            : await ResolveDriverUserIdAsync(notification.DriverId, cancellationToken);

        if (trip is null)
        {
            trip = TripPlanSync.Create(notification, driverUserId);
            db.Trips.Add(trip);
        }
        else
        {
            db.TripStops.AddRange(TripPlanSync.Refresh(trip, notification, driverUserId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Şoför bulunamazsa olay dead-letter kuyruğuna düşer (yeniden işlenebilir).
    private async Task<Guid> ResolveDriverUserIdAsync(Guid driverId, CancellationToken cancellationToken)
        => await driverClient.GetUserIdAsync(driverId, cancellationToken)
           ?? throw new NotFoundException("Driver", driverId);
}
