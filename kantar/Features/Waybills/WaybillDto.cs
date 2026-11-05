using Kantar.Entities;

namespace Kantar.Features.Waybills;

/// <summary>Waybill veri transfer nesnesi.</summary>
public sealed class WaybillDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>WaybillNo.</summary>
    public string WaybillNo { get; set; } = string.Empty;
    /// <summary>WaybillType.</summary>
    public WaybillWaybillType WaybillType { get; set; }
    /// <summary>IssuedAt.</summary>
    public DateTimeOffset IssuedAt { get; set; }
    /// <summary>QuantityKg.</summary>
    public decimal? QuantityKg { get; set; }
    /// <summary>DeliveryAddress.</summary>
    public string? DeliveryAddress { get; set; }
    /// <summary>EDespatchUuid.</summary>
    public Guid? EDespatchUuid { get; set; }
    /// <summary>EDespatchStatus.</summary>
    public WaybillEDespatchStatus? EDespatchStatus { get; set; }
    /// <summary>EDespatchProviderKind.</summary>
    public WaybillEDespatchProviderKind? EDespatchProviderKind { get; set; }
    /// <summary>EDespatchProviderRef.</summary>
    public string? EDespatchProviderRef { get; set; }
    /// <summary>EDespatchReceiverAlias.</summary>
    public string? EDespatchReceiverAlias { get; set; }
    /// <summary>EDespatchMessage.</summary>
    public string? EDespatchMessage { get; set; }
    /// <summary>EDespatchSentAt.</summary>
    public DateTimeOffset? EDespatchSentAt { get; set; }
    /// <summary>EDespatchCheckedAt.</summary>
    public DateTimeOffset? EDespatchCheckedAt { get; set; }
    /// <summary>UBL XML üretilmiş mi (XML'in kendisi DTO'da taşınmaz; elle).</summary>
    public bool HasUbl { get; set; }
    /// <summary>DriverName.</summary>
    public string? DriverName { get; set; }
    /// <summary>Maskeli şoför TCKN'si (ilk 3 ve son 2 hane; tam TCKN DTO'da taşınmaz; elle).</summary>
    public string? DriverNationalIdMasked { get; set; }
    /// <summary>Plate.</summary>
    public string? Plate { get; set; }
    /// <summary>TrailerPlate.</summary>
    public string? TrailerPlate { get; set; }
    /// <summary>ErpSyncStatus.</summary>
    public WaybillErpSyncStatus ErpSyncStatus { get; set; } = WaybillErpSyncStatus.Pending;
    /// <summary>ErpDocNo.</summary>
    public string? ErpDocNo { get; set; }
    /// <summary>ErpSyncedAt.</summary>
    public DateTimeOffset? ErpSyncedAt { get; set; }
    /// <summary>ErpError.</summary>
    public string? ErpError { get; set; }
    /// <summary>WeighingId.</summary>
    public Guid WeighingId { get; set; }
    /// <summary>CompanyId.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>DeliveryLocationId.</summary>
    public Guid? DeliveryLocationId { get; set; }

    /// <summary>Bir Waybill entity'sinden DTO üretir.</summary>
    public static WaybillDto From(Waybill entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new WaybillDto
        {
            Id = entity.Id,
            WaybillNo = entity.WaybillNo,
            WaybillType = entity.WaybillType,
            IssuedAt = entity.IssuedAt,
            QuantityKg = entity.QuantityKg,
            DeliveryAddress = entity.DeliveryAddress,
            EDespatchUuid = entity.EDespatchUuid,
            EDespatchStatus = entity.EDespatchStatus,
            EDespatchProviderKind = entity.EDespatchProviderKind,
            EDespatchProviderRef = entity.EDespatchProviderRef,
            EDespatchReceiverAlias = entity.EDespatchReceiverAlias,
            EDespatchMessage = entity.EDespatchMessage,
            EDespatchSentAt = entity.EDespatchSentAt,
            EDespatchCheckedAt = entity.EDespatchCheckedAt,
            HasUbl = !string.IsNullOrEmpty(entity.UblXml),
            DriverName = entity.DriverName,
            DriverNationalIdMasked = Kantar.Custom.Validation.NationalIdMask.Mask(entity.DriverNationalId),
            Plate = entity.Plate,
            TrailerPlate = entity.TrailerPlate,
            ErpSyncStatus = entity.ErpSyncStatus,
            ErpDocNo = entity.ErpDocNo,
            ErpSyncedAt = entity.ErpSyncedAt,
            ErpError = entity.ErpError,
            WeighingId = entity.WeighingId,
            CompanyId = entity.CompanyId,
            DeliveryLocationId = entity.DeliveryLocationId,
        };
    }
}
