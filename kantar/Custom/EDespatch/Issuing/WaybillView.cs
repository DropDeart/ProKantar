using BaseForge.Core.Exceptions;
using Kantar.Custom.Validation;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>İrsaliye ekranı görünümü; UBL XML ve tam TCKN dönmez.</summary>
public sealed record WaybillView
{
    /// <summary>Kimlik.</summary>
    public Guid Id { get; init; }

    /// <summary>Belge numarası.</summary>
    public string WaybillNo { get; init; } = string.Empty;

    /// <summary>Kâğıt ya da e-İrsaliye.</summary>
    public WaybillWaybillType WaybillType { get; init; }

    /// <summary>Düzenleme anı.</summary>
    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>Miktar (net kg).</summary>
    public decimal? QuantityKg { get; init; }

    /// <summary>Tartım.</summary>
    public Guid WeighingId { get; init; }

    /// <summary>Kantar fişi no.</summary>
    public string? TicketNo { get; init; }

    /// <summary>Alıcı unvanı.</summary>
    public string? CompanyName { get; init; }

    /// <summary>Teslim adresi (tek satır).</summary>
    public string? DeliveryAddress { get; init; }

    /// <summary>Araç plakası.</summary>
    public string? Plate { get; init; }

    /// <summary>Şoför adı soyadı.</summary>
    public string? DriverName { get; init; }

    /// <summary>Maskeli TCKN (ilk 3, son 2 hane).</summary>
    public string? DriverNationalIdMasked { get; init; }

    /// <summary>Kesildiği yöntem.</summary>
    public WaybillEDespatchProviderKind? ProviderKind { get; init; }

    /// <summary>ETTN.</summary>
    public Guid? EDespatchUuid { get; init; }

    /// <summary>e-İrsaliye durumu (kâğıtta null).</summary>
    public WaybillEDespatchStatus? EDespatchStatus { get; init; }

    /// <summary>Sağlayıcı kayıt no.</summary>
    public string? ProviderRef { get; init; }

    /// <summary>Sağlayıcının son açıklaması.</summary>
    public string? Message { get; init; }

    /// <summary>Gönderim anı.</summary>
    public DateTimeOffset? SentAt { get; init; }

    /// <summary>Son durum sorgusu.</summary>
    public DateTimeOffset? CheckedAt { get; init; }

    /// <summary>UBL indirilebilir mi.</summary>
    public bool HasUbl { get; init; }
}

/// <summary><see cref="WaybillView"/> okuma (kiracı filtresi EF query filter'ından).</summary>
public static class WaybillViews
{
    /// <summary>İrsaliyeyi kimliğiyle getirir; yoksa 404.</summary>
    public static async Task<WaybillView> GetAsync(KantarDbContext db, Guid waybillId, CancellationToken ct)
        => await FindAsync(db, db.Waybills.Where(w => w.Id == waybillId), ct)
           ?? throw new NotFoundException(nameof(Waybill), waybillId);

    /// <summary>Tartımın irsaliyesi: önce geçerli olan, yoksa en son reddedilen; hiç yoksa null.</summary>
    public static Task<WaybillView?> FindByWeighingAsync(KantarDbContext db, Guid weighingId, CancellationToken ct)
        => FindAsync(db, db.Waybills
            .Where(w => w.WeighingId == weighingId)
            .OrderBy(w => w.EDespatchStatus == WaybillEDespatchStatus.Rejected)
            .ThenByDescending(w => w.IssuedAt), ct);

    private static async Task<WaybillView?> FindAsync(KantarDbContext db, IQueryable<Waybill> query, CancellationToken ct)
    {
        var row = await query.AsNoTracking()
            .Select(w => new
            {
                View = new WaybillView
                {
                    Id = w.Id,
                    WaybillNo = w.WaybillNo,
                    WaybillType = w.WaybillType,
                    IssuedAt = w.IssuedAt,
                    QuantityKg = w.QuantityKg,
                    WeighingId = w.WeighingId,
                    DeliveryAddress = w.DeliveryAddress,
                    Plate = w.Plate,
                    DriverName = w.DriverName,
                    ProviderKind = w.EDespatchProviderKind,
                    EDespatchUuid = w.EDespatchUuid,
                    EDespatchStatus = w.EDespatchStatus,
                    ProviderRef = w.EDespatchProviderRef,
                    Message = w.EDespatchMessage,
                    SentAt = w.EDespatchSentAt,
                    CheckedAt = w.EDespatchCheckedAt,
                    HasUbl = w.UblXml != null,
                },
                w.DriverNationalId,
                w.CompanyId,
                w.TenantId,
            })
            .FirstOrDefaultAsync(ct);
        if (row is null)
        {
            return null;
        }

        // Fiş ve cari adı snapshot değil; sonradan silinmiş kayıttan da okunur (aynı kiracıda).
        var ticketNo = await db.Weighings.IgnoreQueryFilters()
            .Where(x => x.Id == row.View.WeighingId && x.TenantId == row.TenantId).Select(x => x.TicketNo).FirstOrDefaultAsync(ct);
        var companyName = row.CompanyId is { } companyId
            ? await db.Companies.IgnoreQueryFilters()
                .Where(c => c.Id == companyId && c.TenantId == row.TenantId).Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;
        return row.View with
        {
            TicketNo = ticketNo,
            CompanyName = companyName,
            DriverNationalIdMasked = NationalIdMask.Mask(row.DriverNationalId),
        };
    }
}
