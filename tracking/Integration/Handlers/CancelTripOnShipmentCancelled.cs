using BaseForge.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracking.Data;
using Tracking.Data.Pings;
using Tracking.Integration.Events;

namespace Tracking.Integration.Handlers;

/// <summary>Sevkiyat iptal edilince açık seferi kapatır; şoförün uygulamasından düşer, konum kabul edilmez.</summary>
internal sealed class CancelTripOnShipmentCancelled(
    TrackingDbContext db,
    ITripRouteStore routes,
    IUnitOfWork unitOfWork) : INotificationHandler<ShipmentCancelledEvent>
{
    public async Task Handle(ShipmentCancelledEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var trip = await db.Trips.FirstOrDefaultAsync(
            t => t.Id == notification.ShipmentId && t.TenantId == notification.TenantId, cancellationToken);

        // Sefer hiç açılmadıysa ya da zaten kapandıysa (yeniden teslim) yapılacak bir şey yok.
        if (trip is null || !trip.Cancel(notification.CancelledAt))
        {
            return;
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        // Yola çıkılmışsa gidilen yol saklansın; ham konumlar saklama süresi dolunca silinir.
        if (trip.StartedAt is { } startedAt)
        {
            await routes.RecordAsync(trip.Id, startedAt, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
