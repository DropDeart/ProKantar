using BaseForge.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracking.Data;
using Tracking.Domain;
using Tracking.Entities;
using Tracking.Integration.Events;

namespace Tracking.Integration.Handlers;

/// <summary>Tartım tamamlanınca seferin sıradaki kantar durağını onaylar.</summary>
internal sealed class ConfirmWeighbridgeOnWeighingCompleted(
    TrackingDbContext db,
    ITripCloser tripCloser,
    IUnitOfWork unitOfWork) : INotificationHandler<WeighingCompletedEvent>
{
    public async Task Handle(WeighingCompletedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (notification.ShipmentId is not { } tripId)
        {
            return;
        }

        // Plansız çıkışta sevkiyat tartımdan sonra açılır; sefer henüz yoksa kantar durağı GPS ile kapanır.
        var trip = await db.Trips
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t => t.Id == tripId && t.TenantId == notification.TenantId, cancellationToken);

        // Olay yeniden teslim edildiyse (inbox kaydından önce çökme) aynı tartım ikinci durağı kapatmasın.
        if (trip is null || trip.Stops.Any(s => s.ConfirmedByWeighingId == notification.WeighingId))
        {
            return;
        }

        var stop = trip.Stops
            .Where(s => s.StopType == StopType.Weighbridge && s.Status != StopStatus.Skipped && s.ConfirmedAt is null)
            .MinBy(s => s.Sequence);
        if (!trip.IsOpen || stop is null)
        {
            return;
        }

        stop.ConfirmByWeighing(notification.WeighingId, notification.CompletedAt);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await tripCloser.CloseIfDoneAsync(trip, notification.CompletedAt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
