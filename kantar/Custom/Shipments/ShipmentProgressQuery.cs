using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Kantar.Custom.EDespatch.Issuing;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Shipments;

/// <summary>Sevkiyatta planlanan ile tartılan yan yana: durak başına plan ve irsaliye, tartımlar ve sapma.</summary>
public sealed class ShipmentProgressQuery : IQuery<ShipmentProgressDto>
{
    /// <summary>Sevkiyat kimliği.</summary>
    public Guid ShipmentId { get; set; }
}

/// <summary>Plan / tartım özeti.</summary>
/// <param name="PlannedKg">Plan toplamı (kg); bir satır kg'a çevrilemiyorsa (yoğunluk tanımsız) null.</param>
/// <param name="WeighedNetKg">Tamamlanmış çıkış tartımlarının neti; tartım yoksa null.</param>
/// <param name="DeviationPercent">Net ile plan arasındaki fark (%; işaretli, net büyükse artı).</param>
/// <param name="DeviationWarning">Fark irsaliye uyarı eşiğini (%2) aşıyor mu?</param>
/// <param name="Weighings">Sevkiyatın tartımları.</param>
/// <param name="Stops">Duraklar.</param>
public sealed record ShipmentProgressDto(
    decimal? PlannedKg, decimal? WeighedNetKg, decimal? DeviationPercent, bool DeviationWarning,
    IReadOnlyList<ShipmentProgressWeighing> Weighings, IReadOnlyList<ShipmentProgressStop> Stops);

/// <summary>Sevkiyatın tartımı.</summary>
public sealed record ShipmentProgressWeighing(
    Guid Id, string TicketNo, WeighingDirection Direction, WeighingStatus Status, decimal FirstWeightKg, decimal? NetKg,
    DateTimeOffset FirstWeighedAt, DateTimeOffset? SecondWeighedAt);

/// <summary>Durağın planı ve irsaliyesi.</summary>
/// <param name="PlannedKg">Durak yükünün kg karşılığı; çevrilemiyorsa null.</param>
/// <param name="WaybillId">Durağa kesilen irsaliye.</param>
/// <param name="WaybillValid">İrsaliye geçerli mi (iptal/red değil)?</param>
public sealed record ShipmentProgressStop(
    Guid StopId, int Sequence, ShipmentStopStopType StopType, string Name, Guid? DeliveryLocationId, decimal? PlannedKg,
    Guid? WaybillId, string? WaybillNo, bool WaybillValid);

internal sealed class ShipmentProgressHandler(KantarDbContext db) : IQueryHandler<ShipmentProgressQuery, ShipmentProgressDto>
{
    public async Task<ShipmentProgressDto> Handle(ShipmentProgressQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!await db.Shipments.AnyAsync(s => s.Id == request.ShipmentId, cancellationToken))
        {
            throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        }

        var stops = await db.ShipmentStops.AsNoTracking()
            .Where(s => s.ShipmentId == request.ShipmentId)
            .OrderBy(s => s.Sequence)
            .Select(s => new { s.Id, s.Sequence, s.StopType, s.Name, s.DeliveryLocationId, s.WaybillId })
            .ToListAsync(cancellationToken);
        // Duraklar kiracı filtresinden geçti; satırlar onlara göre süzülür (silinmiş malzeme de okunur).
        var stopIds = stops.Select(s => s.Id).ToList();
        var lines = await db.ShipmentStopLines.IgnoreQueryFilters().AsNoTracking()
            .Where(l => !l.IsDeleted && stopIds.Contains(l.ShipmentStopId))
            .Select(l => new { Line = l, l.Material })
            .ToListAsync(cancellationToken);
        var waybillIds = stops.Where(s => s.WaybillId is not null).Select(s => s.WaybillId!.Value).ToList();
        var waybills = await db.Waybills.AsNoTracking().Where(w => waybillIds.Contains(w.Id))
            .Select(w => new { w.Id, w.WaybillNo })
            .ToListAsync(cancellationToken);
        var valid = (await db.Waybills.AsNoTracking().Where(w => waybillIds.Contains(w.Id)).Where(WaybillStates.IsValid)
            .Select(w => w.Id).ToListAsync(cancellationToken)).ToHashSet();
        var weighings = await db.Weighings.AsNoTracking()
            .Where(w => w.ShipmentId == request.ShipmentId)
            .OrderBy(w => w.FirstWeighedAt)
            .Select(w => new ShipmentProgressWeighing(
                w.Id, w.TicketNo, w.Direction, w.Status, w.FirstWeightKg, w.NetKg, w.FirstWeighedAt, w.SecondWeighedAt))
            .ToListAsync(cancellationToken);

        decimal? KgOfStop(Guid stopId)
        {
            var kgs = lines.Where(x => x.Line.ShipmentStopId == stopId)
                .Select(x => x.Material is null ? null : ShipmentLoadCapacity.KgOf(x.Line, x.Material)).ToList();
            return kgs.Count == 0 ? null : kgs.All(k => k is not null) ? kgs.Sum() : null;
        }

        var stopRows = stops.Select(s =>
        {
            var waybill = waybills.FirstOrDefault(w => w.Id == s.WaybillId);
            return new ShipmentProgressStop(s.Id, s.Sequence, s.StopType, s.Name, s.DeliveryLocationId,
                s.StopType == ShipmentStopStopType.Delivery ? KgOfStop(s.Id) : null,
                waybill?.Id, waybill?.WaybillNo, waybill is not null && valid.Contains(waybill.Id));
        }).ToList();

        var deliveryKgs = stopRows.Where(s => s.StopType == ShipmentStopStopType.Delivery && lines.Any(l => l.Line.ShipmentStopId == s.StopId))
            .Select(s => s.PlannedKg).ToList();
        decimal? plannedKg = deliveryKgs.Count > 0 && deliveryKgs.All(k => k is not null) ? deliveryKgs.Sum() : null;
        var completed = weighings.Where(w => w.Direction == WeighingDirection.Outbound && w.Status == WeighingStatus.Completed && w.NetKg is not null).ToList();
        decimal? netKg = completed.Count > 0 ? completed.Sum(w => w.NetKg!.Value) : null;
        decimal? deviation = plannedKg is > 0 && netKg is not null ? Math.Round((netKg.Value - plannedKg.Value) / plannedKg.Value * 100m, 1) : null;
        var warning = deviation is not null && Math.Abs(deviation.Value) > DespatchDraftBuilder.DeviationThreshold * 100m;

        return new ShipmentProgressDto(plannedKg, netKg, deviation, warning, weighings, stopRows);
    }
}
