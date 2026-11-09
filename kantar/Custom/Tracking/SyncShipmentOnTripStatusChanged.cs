using BaseForge.Core.Interfaces;
using Kantar.Data;
using Kantar.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Tracking;

/// <summary>
/// Sefer yola çıkınca sevkiyatı <see cref="ShipmentStatus.InProgress"/>, bitince <see cref="ShipmentStatus.Completed"/>
/// yapar. Durum yalnız ileri gider: tekrar ya da sırasız gelen olay etkisizdir, iptal/taslak sevkiyata dokunulmaz.
/// </summary>
internal sealed class SyncShipmentOnTripStatusChanged(KantarDbContext db, IUnitOfWork unitOfWork, Notices.TenantNotices notices)
    : INotificationHandler<TripStatusChangedEvent>
{
    public async Task Handle(TripStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (Target(notification.Status) is not { } target)
        {
            return;
        }

        // Aynı anda gelen iptal ile yarışmasın: satır kilitlenir, durum kilit altında okunur.
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Shipments\" WHERE \"Id\" = {notification.TripId} FOR UPDATE", cancellationToken);

        // Olay işlenirken istek kiracısı olmayabilir; kiracı olaydan açıkça süzülür.
        var shipment = await db.Shipments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == notification.TripId && s.TenantId == notification.TenantId && !s.IsDeleted, cancellationToken);
        if (shipment is not null && CanAdvance(shipment.Status, target))
        {
            shipment.Status = target;
            if (target == ShipmentStatus.Completed)
            {
                await notices.ShipmentCompletedAsync(shipment.TenantId, shipment.Id, shipment.ShipmentNo, shipment.Plate, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <summary>Sefer durumunun sevkiyattaki karşılığı; karşılığı yoksa null.</summary>
    private static ShipmentStatus? Target(TripProgress status) => status switch
    {
        TripProgress.Active => ShipmentStatus.InProgress,
        TripProgress.Completed => ShipmentStatus.Completed,
        _ => null,
    };

    /// <summary>Yalnız yayınlanmış/yoldaki sevkiyat ileri bir duruma geçer.</summary>
    private static bool CanAdvance(ShipmentStatus current, ShipmentStatus target) => current switch
    {
        ShipmentStatus.Dispatched => target is ShipmentStatus.InProgress or ShipmentStatus.Completed,
        ShipmentStatus.InProgress => target is ShipmentStatus.Completed,
        _ => false,
    };
}
