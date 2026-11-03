using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>ShipmentStop entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class ShipmentStop : BaseEntity, ITenantEntity
{
    /// <summary>Sequence.</summary>
    public int Sequence { get; set; }
    /// <summary>StopType.</summary>
    public ShipmentStopStopType StopType { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Latitude.</summary>
    public decimal Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 150;
    /// <summary>PlannedAt.</summary>
    public DateTimeOffset? PlannedAt { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>ShipmentId.</summary>
    public Guid ShipmentId { get; set; }
    /// <summary>DeliveryLocationId.</summary>
    public Guid? DeliveryLocationId { get; set; }
    /// <summary>StationId.</summary>
    public Guid? StationId { get; set; }
    /// <summary>WaybillId.</summary>
    public Guid? WaybillId { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Shipment (servis içi ilişki).</summary>
    public Shipment? Shipment { get; set; }
    /// <summary>DeliveryLocation (servis içi ilişki).</summary>
    public DeliveryLocation? DeliveryLocation { get; set; }
    /// <summary>Station (servis içi ilişki).</summary>
    public Station? Station { get; set; }
    /// <summary>Waybill (servis içi ilişki).</summary>
    public Waybill? Waybill { get; set; }
}
