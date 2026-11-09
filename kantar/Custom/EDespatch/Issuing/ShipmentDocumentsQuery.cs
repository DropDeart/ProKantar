using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Custom.EDespatch.Parties;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>
/// Sevkiyatın irsaliyeleri, teslimat durağı başına: kesilmişse irsaliye; kesilmemişse plandan hesaplanan taslak
/// (numarasız; GİB numarası sıralı olduğu için taslak numara tüketmez). Taslak çıkış tartımında tartılan netle kesinleşir.
/// </summary>
public sealed class ShipmentDocumentsQuery : IQuery<IReadOnlyList<ShipmentDocument>>
{
    /// <summary>Sevkiyat.</summary>
    public Guid ShipmentId { get; set; }
}

/// <summary>Durağın belgesi.</summary>
/// <param name="Issued">İrsaliye kesildi mi (false: taslak)?</param>
/// <param name="WeighingId">Çıkış tartımı (yoksa null; taslak tartımdan sonra kesinleşir).</param>
/// <param name="Missing">Kesmeyi engelleyen eksikler (yalnız taslakta).</param>
/// <param name="Warnings">Kesmeyi engellemeyen uyarılar.</param>
public sealed record ShipmentDocument(
    Guid StopId, int Sequence, string StopName, Guid? DeliveryLocationId, bool Issued,
    Guid? WaybillId, string? WaybillNo, WaybillWaybillType? WaybillType, WaybillEDespatchStatus? EDespatchStatus, DateTimeOffset? IssuedAt,
    Guid? WeighingId, string? TicketNo, string? SupplierName, string? StationName, string? CustomerName, string DeliveryAddress,
    string Plate, string? DriverName, IReadOnlyList<DespatchPreviewLine> Lines, decimal? QuantityKg,
    IReadOnlyList<DespatchRuleError> Missing, IReadOnlyList<string> Warnings);

internal sealed class ShipmentDocumentsHandler(
    KantarDbContext db, ICurrentTenant currentTenant, DespatchDraftBuilder drafts, ISupplierDirectory suppliers)
    : IQueryHandler<ShipmentDocumentsQuery, IReadOnlyList<ShipmentDocument>>
{
    public async Task<IReadOnlyList<ShipmentDocument>> Handle(ShipmentDocumentsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = currentTenant.RequireTenantId();
        var shipment = await db.Shipments.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        var stops = await db.ShipmentStops.AsNoTracking()
            .Where(s => s.ShipmentId == shipment.Id && s.StopType == ShipmentStopStopType.Delivery && s.DeliveryLocationId != null)
            .OrderBy(s => s.Sequence)
            .ToListAsync(cancellationToken);
        if (stops.Count == 0)
        {
            return [];
        }

        // Tamamlanmış çıkış tartımı varsa taslak onun netiyle (tek durakta gerçek net), yoksa planla kurulur.
        var weighing = await db.Weighings.AsNoTracking()
            .Where(w => w.ShipmentId == shipment.Id && w.Direction == WeighingDirection.Outbound && w.Status != WeighingStatus.Cancelled)
            .OrderByDescending(w => w.Status == WeighingStatus.Completed).ThenByDescending(w => w.FirstWeighedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var basis = weighing is { Status: WeighingStatus.Completed } ? weighing : PlannedWeighing(shipment, tenantId);
        var waybillIds = stops.Where(s => s.WaybillId is not null).Select(s => s.WaybillId!.Value).ToList();
        var waybills = await db.Waybills.AsNoTracking().Where(w => waybillIds.Contains(w.Id)).Where(WaybillStates.IsValid)
            .ToDictionaryAsync(w => w.Id, cancellationToken);
        var supplier = (await suppliers.GetAsync(tenantId, cancellationToken))?.Name;

        var result = new List<ShipmentDocument>(stops.Count);
        foreach (var stop in stops)
        {
            var waybill = stop.WaybillId is { } id && waybills.TryGetValue(id, out var w) ? w : null;
            var draft = await drafts.BuildAsync(basis, new DespatchDraftInput(
                tenantId, waybill?.WaybillNo ?? "TASLAK", waybill?.EDespatchUuid ?? Guid.NewGuid(), waybill?.IssuedAt ?? DateTimeOffset.UtcNow,
                null, stop.DeliveryLocationId, null), cancellationToken);
            var document = draft.Document;
            var missing = waybill is not null
                ? []
                : DespatchValidation.Check(document).Where(e => !e.Field.StartsWith("DocumentNo", StringComparison.Ordinal)).ToList();

            result.Add(new ShipmentDocument(
                stop.Id, stop.Sequence, stop.Name, stop.DeliveryLocationId, waybill is not null,
                waybill?.Id, waybill?.WaybillNo, waybill?.WaybillType, waybill?.EDespatchStatus, waybill?.IssuedAt,
                weighing?.Id, weighing?.TicketNo, string.IsNullOrWhiteSpace(supplier) ? null : supplier, document.Station?.Name,
                string.IsNullOrWhiteSpace(document.Customer.Name) ? null : document.Customer.Name, draft.DeliveryAddressText,
                DespatchRules.NormalizePlate(document.Plate), draft.DriverName,
                document.Lines.Select(l => new DespatchPreviewLine(l.Name, l.Code, l.Quantity, DespatchUnitCodes.Of(l.Unit))).ToList(),
                waybill?.QuantityKg ?? draft.QuantityKg, missing, draft.Warnings));
        }

        return result;
    }

    /// <summary>Henüz tartılmamış sevkiyat için taslağın dayanağı: plandaki araç, şoför ve istasyon (net yok).</summary>
    private static Weighing PlannedWeighing(Shipment shipment, Guid tenantId) => new()
    {
        Id = Guid.Empty,
        TenantId = tenantId,
        ShipmentId = shipment.Id,
        Direction = WeighingDirection.Outbound,
        Status = WeighingStatus.Open,
        Plate = shipment.Plate,
        DriverId = shipment.DriverId,
        DriverName = shipment.DriverName,
        StationId = shipment.StationId,
        VehicleId = shipment.VehicleId,
        TicketNo = string.Empty,
    };
}
