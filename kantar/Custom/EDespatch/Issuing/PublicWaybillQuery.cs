using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Kantar.Custom.EDespatch.Parties;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>
/// Şoföre giden linkteki irsaliye (giriş istemez). Kiracı irsaliye kaydından gelir; bütün okumalar kiracıyla açıkça süzülür.
/// TCKN ve UBL dönmez.
/// </summary>
public sealed class PublicWaybillQuery : IQuery<PublicWaybillView>
{
    /// <summary>Linkteki imzalı token.</summary>
    public string Token { get; set; } = string.Empty;
}

/// <summary>Salt okunur irsaliye.</summary>
public sealed record PublicWaybillView(
    string WaybillNo, WaybillWaybillType WaybillType, DateTimeOffset IssuedAt, bool Valid, WaybillEDespatchStatus? EDespatchStatus,
    string? SupplierName, string? CustomerName, string? DeliveryAddress, string Plate, string? TrailerPlate, string? DriverName,
    string? TicketNo, string? ShipmentNo, decimal? NetKg, IReadOnlyList<DespatchPreviewLine> Lines, DateTimeOffset LinkExpiresAt);

internal sealed class PublicWaybillHandler(
    KantarDbContext db, WaybillLinks links, DespatchDraftBuilder drafts, ISupplierDirectory suppliers)
    : IQueryHandler<PublicWaybillQuery, PublicWaybillView>
{
    public async Task<PublicWaybillView> Handle(PublicWaybillQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Süresi dolmuş, bozuk ya da silinmiş: hepsi aynı 404 (token'ın neden geçersiz olduğu söylenmez).
        var waybillId = await links.TryReadAsync(request.Token, cancellationToken) ?? throw new NotFoundException("Waybill", request.Token);
        var waybill = await db.Waybills.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == waybillId && !w.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Waybill", request.Token);
        var tenantId = waybill.TenantId;
        var weighing = await db.Weighings.IgnoreQueryFilters().AsNoTracking()
            .FirstAsync(w => w.Id == waybill.WeighingId && w.TenantId == tenantId, cancellationToken);
        var shipmentNo = weighing.ShipmentId is { } shipmentId
            ? await db.Shipments.IgnoreQueryFilters().Where(s => s.Id == shipmentId && s.TenantId == tenantId)
                .Select(s => s.ShipmentNo).FirstOrDefaultAsync(cancellationToken)
            : null;
        var customer = waybill.CompanyId is { } companyId
            ? await db.Companies.IgnoreQueryFilters().Where(c => c.Id == companyId && c.TenantId == tenantId)
                .Select(c => c.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var supplier = (await suppliers.GetAsync(tenantId, cancellationToken))?.Name;
        var lines = await drafts.LinesOfWaybillAsync(waybill.Id, weighing, tenantId, cancellationToken);
        var valid = WaybillStates.IsValid.Compile()(waybill);

        return new PublicWaybillView(
            waybill.WaybillNo, waybill.WaybillType, waybill.IssuedAt, valid, waybill.EDespatchStatus,
            string.IsNullOrWhiteSpace(supplier) ? null : supplier, customer, waybill.DeliveryAddress, waybill.Plate ?? weighing.Plate, waybill.TrailerPlate,
            waybill.DriverName, weighing.TicketNo, shipmentNo, weighing.NetKg,
            lines.Select(l => new DespatchPreviewLine(l.Name, l.Code, l.Quantity, DespatchUnitCodes.Of(l.Unit))).ToList(),
            WaybillLinks.ExpiresAt(waybill.IssuedAt));
    }
}
