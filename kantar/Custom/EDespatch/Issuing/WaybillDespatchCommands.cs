using System.Text;
using System.Text.Json.Serialization;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using BaseForge.Core.Serialization;
using Kantar.Custom.EDespatch.Providers;
using Kantar.Custom.EDespatch.Settings;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>Tartımın bütün irsaliyeleri (durak başına).</summary>
public sealed class ListWeighingDespatchesQuery : IQuery<IReadOnlyList<WaybillView>>
{
    /// <summary>Tartım.</summary>
    public Guid WeighingId { get; set; }
}

internal sealed class ListWeighingDespatchesHandler(KantarDbContext db) : IQueryHandler<ListWeighingDespatchesQuery, IReadOnlyList<WaybillView>>
{
    public Task<IReadOnlyList<WaybillView>> Handle(ListWeighingDespatchesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return WaybillViews.ListByWeighingAsync(db, request.WeighingId, cancellationToken);
    }
}

/// <summary>Tartımın irsaliyesi; yoksa null (404).</summary>
public sealed class GetWeighingDespatchQuery : IQuery<WaybillView?>
{
    /// <summary>Tartım.</summary>
    public Guid WeighingId { get; set; }
}

/// <summary>UBL dosyası.</summary>
/// <param name="FileName">İndirme adı ({WaybillNo}.xml).</param>
/// <param name="Content">UTF-8 XML.</param>
public sealed record UblFile(string FileName, byte[] Content);

/// <summary>İrsaliyenin UBL XML'i; UBL üretilmemişse 404.</summary>
public sealed class GetWaybillUblQuery : IQuery<UblFile>
{
    /// <summary>İrsaliye.</summary>
    public Guid WaybillId { get; set; }
}

/// <summary>Sağlayıcıdan güncel durumu çeker (elle yüklemede durum değişmez, yalnız elle işaretlenir).</summary>
public sealed class RefreshWaybillStatusCommand : ICommand<WaybillView>
{
    /// <summary>İrsaliye.</summary>
    public Guid WaybillId { get; set; }
}

/// <summary>Elle işaretlenebilen durumlar.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<ManualDespatchMark>))]
public enum ManualDespatchMark
{
    /// <summary>Portal/entegratöre yüklendi.</summary>
    Sent,

    /// <summary>Alıcıya ulaştı.</summary>
    Delivered,

    /// <summary>Alıcı reddetti.</summary>
    Rejected,
}

/// <summary>Elle yüklenen (ManualXml) irsaliyenin durumunu işaretler.</summary>
public sealed class MarkWaybillCommand : ICommand<WaybillView>
{
    /// <summary>İrsaliye (route'tan).</summary>
    public Guid WaybillId { get; set; }

    /// <summary>Yeni durum.</summary>
    public ManualDespatchMark Status { get; set; }

    /// <summary>Açıklama.</summary>
    public string? Note { get; set; }
}

/// <summary>Başarısız e-Logo gönderimini yeniden dener.</summary>
public sealed class ResendWaybillCommand : ICommand<WaybillView>
{
    /// <summary>İrsaliye.</summary>
    public Guid WaybillId { get; set; }
}

internal sealed class GetWeighingDespatchHandler(KantarDbContext db) : IQueryHandler<GetWeighingDespatchQuery, WaybillView?>
{
    public Task<WaybillView?> Handle(GetWeighingDespatchQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return WaybillViews.FindByWeighingAsync(db, request.WeighingId, cancellationToken);
    }
}

internal sealed class GetWaybillUblHandler(KantarDbContext db) : IQueryHandler<GetWaybillUblQuery, UblFile>
{
    public async Task<UblFile> Handle(GetWaybillUblQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var row = await db.Waybills.AsNoTracking()
            .Where(w => w.Id == request.WaybillId)
            .Select(w => new { w.WaybillNo, w.UblXml })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Waybill), request.WaybillId);
        return row.UblXml is null
            ? throw new NotFoundException("Bu irsaliye için UBL üretilmedi (kâğıt irsaliye).")
            : new UblFile($"{row.WaybillNo}.xml", Encoding.UTF8.GetBytes(row.UblXml));
    }
}

internal sealed class RefreshWaybillStatusHandler(
    KantarDbContext db, IUnitOfWork unitOfWork, ICurrentTenant currentTenant, EDespatchSettingsStore settings, IEDespatchProviderRegistry registry,
    Notices.TenantNotices notices)
    : ICommandHandler<RefreshWaybillStatusCommand, WaybillView>
{
    public async Task<WaybillView> Handle(RefreshWaybillStatusCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var waybill = await WaybillLookup.TrackedAsync(db, request.WaybillId, cancellationToken);
        if (waybill.EDespatchProviderKind == WaybillEDespatchProviderKind.ELogo)
        {
            if (waybill.EDespatchUuid is not { } ettn || waybill.EDespatchStatus is WaybillEDespatchStatus.Draft or WaybillEDespatchStatus.Failed)
            {
                throw new ValidationException(nameof(Waybill.EDespatchStatus), "Belge henüz sağlayıcıya iletilmedi; önce yeniden gönderin.");
            }

            var config = await settings.ResolveAsync(currentTenant.RequireTenantId(), cancellationToken);
            var result = await registry.Get(EDespatchProviderKind.ELogo).GetStatusAsync(ettn, config.Credentials, cancellationToken);
            var rejectedBefore = waybill.EDespatchStatus == WaybillEDespatchStatus.Rejected;
            WaybillStates.ApplyStatus(waybill, result, DateTimeOffset.UtcNow);
            if (rejectedBefore is false && waybill.EDespatchStatus == WaybillEDespatchStatus.Rejected)
            {
                await notices.WaybillRejectedAsync(waybill.TenantId, waybill.Id, waybill.WeighingId, waybill.WaybillNo, waybill.EDespatchMessage, CancellationToken.None);
            }
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await WaybillViews.GetAsync(db, waybill.Id, cancellationToken);
    }
}

internal sealed class MarkWaybillHandler(KantarDbContext db, IUnitOfWork unitOfWork, Notices.TenantNotices notices)
    : ICommandHandler<MarkWaybillCommand, WaybillView>
{
    public async Task<WaybillView> Handle(MarkWaybillCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var waybill = await WaybillLookup.TrackedAsync(db, request.WaybillId, cancellationToken);
        if (waybill.EDespatchProviderKind != WaybillEDespatchProviderKind.ManualXml)
        {
            throw new ValidationException(nameof(Waybill.EDespatchProviderKind), "Durum yalnız elle yüklenen (XML) irsaliyede işaretlenir.");
        }

        var target = request.Status switch
        {
            ManualDespatchMark.Sent => WaybillEDespatchStatus.Sent,
            ManualDespatchMark.Delivered => WaybillEDespatchStatus.Delivered,
            _ => WaybillEDespatchStatus.Rejected,
        };
        if (!CanMove(waybill.EDespatchStatus, target))
        {
            throw new ValidationException(nameof(request.Status), $"İrsaliye {waybill.EDespatchStatus} durumundan {target} durumuna geçemez.");
        }

        var now = DateTimeOffset.UtcNow;
        waybill.EDespatchStatus = target;
        waybill.EDespatchMessage = string.IsNullOrWhiteSpace(request.Note) ? "Durum elle işaretlendi." : request.Note.Trim();
        waybill.EDespatchCheckedAt = now;
        waybill.EDespatchSentAt ??= target is WaybillEDespatchStatus.Sent or WaybillEDespatchStatus.Delivered ? now : null;
        if (target == WaybillEDespatchStatus.Rejected)
        {
            await notices.WaybillRejectedAsync(waybill.TenantId, waybill.Id, waybill.WeighingId, waybill.WaybillNo, waybill.EDespatchMessage, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await WaybillViews.GetAsync(db, waybill.Id, cancellationToken);
    }

    /// <summary>İleri yönlü geçiş: yüklenmeyi bekliyor → gönderildi → teslim; ret her aşamadan, ret sonrası değişmez.</summary>
    private static bool CanMove(WaybillEDespatchStatus? from, WaybillEDespatchStatus to) => (from, to) switch
    {
        (WaybillEDespatchStatus.Rejected, _) => false,
        (_, WaybillEDespatchStatus.Rejected) => true,
        (WaybillEDespatchStatus.AwaitingManualUpload or WaybillEDespatchStatus.Draft, _) => true,
        (WaybillEDespatchStatus.Sent, WaybillEDespatchStatus.Delivered) => true,
        _ => false,
    };
}

internal sealed class ResendWaybillHandler(KantarDbContext db, ICurrentTenant currentTenant, DespatchDelivery delivery)
    : ICommandHandler<ResendWaybillCommand, WaybillView>
{
    /// <summary>Gönderim sonucu yazılamadan kalmış Draft bu süreden sonra yeniden gönderilebilir.</summary>
    private static readonly TimeSpan StaleDraft = TimeSpan.FromMinutes(2);

    public async Task<WaybillView> Handle(ResendWaybillCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = currentTenant.RequireTenantId();
        var waybill = await WaybillLookup.TrackedAsync(db, request.WaybillId, cancellationToken);
        if (waybill.EDespatchProviderKind != WaybillEDespatchProviderKind.ELogo)
        {
            throw new ValidationException(nameof(Waybill.EDespatchProviderKind), "Yeniden gönderim yalnız entegratörle (e-Logo) kesilen irsaliyede yapılır.");
        }

        // Eş zamanlı iki deneme aynı belgeyi iki kez göndermesin: Failed → Draft geçişini tek istek kazanır.
        var staleBefore = DateTimeOffset.UtcNow - StaleDraft;
        var claimed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "Waybills" SET "EDespatchStatus" = 'Draft', "UpdatedAt" = now()
            WHERE "Id" = {waybill.Id} AND "TenantId" = {tenantId} AND NOT "IsDeleted"
              AND ("EDespatchStatus" = 'Failed' OR ("EDespatchStatus" = 'Draft' AND "CreatedAt" < {staleBefore}))
            """, cancellationToken);
        if (claimed == 0)
        {
            throw new ValidationException(nameof(Waybill.EDespatchStatus), "Yalnız gönderimi başarısız olan irsaliye yeniden gönderilir.");
        }

        await db.Entry(waybill).ReloadAsync(cancellationToken);
        await delivery.ResendAsync(waybill, tenantId);
        return await WaybillViews.GetAsync(db, waybill.Id, CancellationToken.None);
    }
}

/// <summary>Kiracı filtreli, takip edilen irsaliye.</summary>
internal static class WaybillLookup
{
    public static async Task<Waybill> TrackedAsync(KantarDbContext db, Guid id, CancellationToken ct)
        => await db.Waybills.FirstOrDefaultAsync(w => w.Id == id, ct) ?? throw new NotFoundException(nameof(Waybill), id);
}
