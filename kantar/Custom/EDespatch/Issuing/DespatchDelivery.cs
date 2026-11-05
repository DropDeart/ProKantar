using System.Text;
using System.Xml.Linq;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Custom.EDespatch.Providers;
using Kantar.Custom.EDespatch.Settings;
using Kantar.Entities;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>
/// Commit edilmiş irsaliyeyi sağlayıcıya iletir ve sonucu ayrı bir kayıtla yazar. Sağlayıcı hatası istisna olarak
/// yukarı çıkmaz: irsaliye <see cref="WaybillEDespatchStatus.Failed"/> kalır, <c>resend</c> ile yeniden denenir.
/// </summary>
public sealed class DespatchDelivery(
    IEDespatchProviderRegistry registry, EDespatchSettingsStore settings, IUnitOfWork unitOfWork, ILogger<DespatchDelivery> logger)
{
    /// <summary>Belgeyi gönderir; istemci bağlantıyı kesse de sonuç yazılsın diye iptal belirteci kullanılmaz.</summary>
    public async Task SendAsync(Waybill waybill, DespatchDocument document, byte[] ublXml, EDespatchCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(waybill);
        var provider = registry.Get(WaybillStates.ToProvider(waybill.EDespatchProviderKind ?? WaybillEDespatchProviderKind.PaperOnly));
        DespatchSendResult result;
        try
        {
            result = await provider.SendAsync(document, ublXml, credentials, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Kayıt commit edildi; hangi hata olursa olsun irsaliye Failed'a düşmeli, Draft'ta asılı kalmamalı.
            logger.LogWarning(ex, "İrsaliye gönderilemedi ({WaybillNo}).", waybill.WaybillNo);
            result = new DespatchSendResult(DespatchState.Failed, ex is BaseException ? ex.Message : "Gönderim sırasında hata oluştu; tekrar deneyin.");
        }

        WaybillStates.ApplySend(waybill, result, DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    /// <summary>Kayıtlı UBL ile yeniden gönderir (kiracının güncel kimlik bilgisiyle).</summary>
    public async Task ResendAsync(Waybill waybill, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(waybill);
        var xml = waybill.UblXml ?? throw new ValidationException(nameof(Waybill.UblXml), "Bu irsaliyede UBL yok.");
        var config = await settings.ResolveAsync(tenantId, CancellationToken.None);
        var bytes = Encoding.UTF8.GetBytes(xml);
        await SendAsync(waybill, StoredDocument.FromUbl(waybill, xml), bytes, config.Credentials);
    }
}

/// <summary>
/// Yeniden gönderimde sağlayıcıya verilecek belge: gönderilen UBL'in kendisi kayıtlıdır, sağlayıcı belgeden yalnız
/// ETTN, numara ve taraf kimliklerini okur. Bunlar kayıtlı UBL'den alınır; veriler sonradan değişse de aynı belge gider.
/// </summary>
internal static class StoredDocument
{
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    public static DespatchDocument FromUbl(Waybill waybill, string xml)
    {
        var root = XDocument.Parse(xml).Root ?? throw new ValidationException(nameof(Waybill.UblXml), "Kayıtlı UBL okunamadı.");
        DespatchParty PartyOf(string element)
        {
            var party = root.Element(Cac + element)?.Element(Cac + "Party");
            return new DespatchParty
            {
                TaxId = party?.Element(Cac + "PartyIdentification")?.Element(Cbc + "ID")?.Value ?? string.Empty,
                Name = party?.Element(Cac + "PartyName")?.Element(Cbc + "Name")?.Value ?? string.Empty,
                Address = new DespatchAddress(),
            };
        }

        return new DespatchDocument
        {
            Ettn = waybill.EDespatchUuid ?? Guid.Parse(root.Element(Cbc + "UUID")?.Value ?? Guid.Empty.ToString()),
            DocumentNo = waybill.WaybillNo,
            IssuedAt = waybill.IssuedAt,
            DespatchedAt = waybill.IssuedAt,
            Supplier = PartyOf("DespatchSupplierParty"),
            Customer = PartyOf("DeliveryCustomerParty"),
            DeliveryAddress = new DespatchAddress(),
            Plate = waybill.Plate ?? string.Empty,
            Lines = [],
        };
    }
}
