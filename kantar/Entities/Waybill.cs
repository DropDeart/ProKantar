using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>Waybill entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Waybill : BaseEntity, ITenantEntity
{
    /// <summary>WaybillNo.</summary>
    [MaxLength(30)]
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
    [MaxLength(100)]
    public string? EDespatchProviderRef { get; set; }
    /// <summary>EDespatchReceiverAlias.</summary>
    [MaxLength(200)]
    public string? EDespatchReceiverAlias { get; set; }
    /// <summary>EDespatchMessage.</summary>
    public string? EDespatchMessage { get; set; }
    /// <summary>EDespatchSentAt.</summary>
    public DateTimeOffset? EDespatchSentAt { get; set; }
    /// <summary>EDespatchCheckedAt.</summary>
    public DateTimeOffset? EDespatchCheckedAt { get; set; }
    /// <summary>UblXml.</summary>
    public string? UblXml { get; set; }
    /// <summary>DriverName.</summary>
    [MaxLength(150)]
    public string? DriverName { get; set; }
    /// <summary>DriverNationalId.</summary>
    [MaxLength(11)]
    public string? DriverNationalId { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string? Plate { get; set; }
    /// <summary>TrailerPlate.</summary>
    [MaxLength(15)]
    public string? TrailerPlate { get; set; }
    /// <summary>ErpSyncStatus.</summary>
    public WaybillErpSyncStatus ErpSyncStatus { get; set; } = WaybillErpSyncStatus.Pending;
    /// <summary>ErpDocNo.</summary>
    [MaxLength(50)]
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
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Weighing (servis içi ilişki).</summary>
    public Weighing? Weighing { get; set; }
    /// <summary>Company (servis içi ilişki).</summary>
    public Company? Company { get; set; }
    /// <summary>DeliveryLocation (servis içi ilişki).</summary>
    public DeliveryLocation? DeliveryLocation { get; set; }
}
