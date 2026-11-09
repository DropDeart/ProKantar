using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Data;
using Kantar.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>
/// Çıkış tartımı tamamlanınca sevkiyatın taslak irsaliyelerini kesinleştirir: geçerli irsaliyesi olmayan her teslimat
/// durağına tartılan netle irsaliye kesilir ve (e-İrsaliyeyse) gönderilir. Eksik bilgi (TCKN, posta kodu vb.) olan durak
/// taslak kalır; eksikler sevkiyat detayında gösterilir ve tamamlanınca elle kesilir. Tartımı geri almaz.
/// </summary>
public sealed partial class ShipmentWaybillIssuer(
    KantarDbContext db, ISender sender, IUnitOfWork unitOfWork, Notices.TenantNotices notices, ILogger<ShipmentWaybillIssuer> logger)
{
    /// <summary>Tamamlanmış çıkış tartımının sevkiyatındaki taslakları keser; kesilen irsaliye sayısını döner.</summary>
    public async Task<int> IssueForWeighingAsync(Guid weighingId, CancellationToken ct)
    {
        var weighing = await db.Weighings.AsNoTracking()
            .Where(w => w.Id == weighingId)
            .Select(w => new { w.ShipmentId, w.Direction, w.Status, w.TicketNo, w.TenantId })
            .FirstOrDefaultAsync(ct);
        if (weighing is not { ShipmentId: { } shipmentId, Direction: WeighingDirection.Outbound, Status: WeighingStatus.Completed })
        {
            return 0;
        }

        var stops = await db.ShipmentStops.AsNoTracking()
            .Where(s => s.ShipmentId == shipmentId && s.StopType == ShipmentStopStopType.Delivery && s.DeliveryLocationId != null)
            .OrderBy(s => s.Sequence)
            .Select(s => new { s.Name, s.DeliveryLocationId, s.WaybillId })
            .ToListAsync(ct);
        var issued = 0;
        var kept = 0;
        foreach (var stop in stops)
        {
            if (stop.WaybillId is { } existing && await db.Waybills.Where(w => w.Id == existing).AnyAsync(WaybillStates.IsValid, ct))
            {
                continue;
            }

            try
            {
                await sender.Send(new IssueDespatchCommand { WeighingId = weighingId, DeliveryLocationId = stop.DeliveryLocationId }, ct);
                issued++;
            }
            catch (ValidationException ex)
            {
                LogDraftKept(logger, weighing.TicketNo, stop.Name, ex.Message);
                await notices.DraftKeptAsync(weighing.TenantId, weighingId, shipmentId, weighing.TicketNo, stop.Name, ex.Message, ct);
                kept++;
            }
        }

        if (kept > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }

        return issued;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fiş {TicketNo}, {Stop}: irsaliye taslak kaldı ({Reason}).")]
    private static partial void LogDraftKept(ILogger logger, string ticketNo, string stop, string reason);
}
