using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>Shipment entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Shipment : BaseEntity, ITenantEntity
{
    /// <summary>ShipmentNo.</summary>
    [MaxLength(30)]
    public string ShipmentNo { get; set; } = string.Empty;
    /// <summary>Direction.</summary>
    public ShipmentDirection Direction { get; set; }
    /// <summary>Status.</summary>
    public ShipmentStatus Status { get; set; }
    /// <summary>PlannedAt.</summary>
    public DateTimeOffset PlannedAt { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>DriverId.</summary>
    public Guid? DriverId { get; set; }
    /// <summary>DriverName.</summary>
    [MaxLength(150)]
    public string? DriverName { get; set; }
    /// <summary>DispatchedAt.</summary>
    public DateTimeOffset? DispatchedAt { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Station (servis içi ilişki).</summary>
    public Station? Station { get; set; }
}
