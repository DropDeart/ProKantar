using System.Linq.Expressions;
using Kantar.Custom.EDespatch.Providers;
using Kantar.Entities;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>Sağlayıcı durumlarının Waybill alanlarına yazılması ve "geçerli irsaliye" tanımı.</summary>
public static class WaybillStates
{
    /// <summary>Geçerli irsaliye: silinmemiş ve alıcı reddetmemiş (reddedilen tartıma yeniden kesilebilir).</summary>
    public static readonly Expression<Func<Waybill, bool>> IsValid =
        w => w.EDespatchStatus == null || w.EDespatchStatus != WaybillEDespatchStatus.Rejected;

    /// <summary>Sağlayıcı türünün Waybill karşılığı.</summary>
    public static WaybillEDespatchProviderKind ToEntity(EDespatchProviderKind kind) => kind switch
    {
        EDespatchProviderKind.PaperOnly => WaybillEDespatchProviderKind.PaperOnly,
        EDespatchProviderKind.ManualXml => WaybillEDespatchProviderKind.ManualXml,
        EDespatchProviderKind.ELogo => WaybillEDespatchProviderKind.ELogo,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Bilinmeyen irsaliye yöntemi."),
    };

    /// <summary>Waybill'deki türün sağlayıcı karşılığı.</summary>
    public static EDespatchProviderKind ToProvider(WaybillEDespatchProviderKind kind) => kind switch
    {
        WaybillEDespatchProviderKind.PaperOnly => EDespatchProviderKind.PaperOnly,
        WaybillEDespatchProviderKind.ManualXml => EDespatchProviderKind.ManualXml,
        WaybillEDespatchProviderKind.ELogo => EDespatchProviderKind.ELogo,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Bilinmeyen irsaliye yöntemi."),
    };

    /// <summary>Sağlayıcı durumunun Waybill karşılığı; kâğıtta null.</summary>
    public static WaybillEDespatchStatus? ToEntity(DespatchState state) => state switch
    {
        DespatchState.NotApplicable => null,
        DespatchState.AwaitingManualUpload => WaybillEDespatchStatus.AwaitingManualUpload,
        DespatchState.Sent => WaybillEDespatchStatus.Sent,
        DespatchState.Delivered => WaybillEDespatchStatus.Delivered,
        DespatchState.Rejected => WaybillEDespatchStatus.Rejected,
        DespatchState.Failed => WaybillEDespatchStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Bilinmeyen durum."),
    };

    /// <summary>Gönderim sonucunu yazar.</summary>
    public static void ApplySend(Waybill waybill, DespatchSendResult result, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(waybill);
        ArgumentNullException.ThrowIfNull(result);
        waybill.EDespatchStatus = ToEntity(result.State);
        waybill.EDespatchMessage = result.Message;
        waybill.EDespatchProviderRef = result.ProviderReference ?? waybill.EDespatchProviderRef;
        waybill.EDespatchReceiverAlias = result.ReceiverAlias ?? waybill.EDespatchReceiverAlias;
        if (result.State is DespatchState.Sent or DespatchState.Delivered)
        {
            waybill.EDespatchSentAt = now;
        }
    }

    /// <summary>
    /// Durum sorgusunu yazar. Ham kodu olmayan <see cref="DespatchState.Failed"/> sorgunun kendisinin başarısız olduğunu
    /// gösterir (ağ/giriş hatası); bu durumda belge durumu korunur, yalnız mesaj güncellenir.
    /// </summary>
    public static void ApplyStatus(Waybill waybill, DespatchStatusResult result, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(waybill);
        ArgumentNullException.ThrowIfNull(result);
        waybill.EDespatchCheckedAt = now;
        waybill.EDespatchMessage = result.Message;
        if (result.State == DespatchState.Failed && result.ProviderCode is null)
        {
            return;
        }

        waybill.EDespatchStatus = ToEntity(result.State);
    }
}
