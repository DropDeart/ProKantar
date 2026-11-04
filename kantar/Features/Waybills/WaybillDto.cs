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
