using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>Weighbridge entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Weighbridge : BaseEntity, ITenantEntity
{
    /// <summary>Name.</summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    /// <summary>SerialNo.</summary>
    [MaxLength(50)]
    public string? SerialNo { get; set; }
    /// <summary>CapacityKg.</summary>
    public decimal CapacityKg { get; set; }
    /// <summary>ConnectionType.</summary>
    public WeighbridgeConnectionType? ConnectionType { get; set; }
    /// <summary>ConnectionInfo.</summary>
    [Column(TypeName = "jsonb")]
    public string? ConnectionInfo { get; set; }
    /// <summary>CalibrationValidUntil.</summary>
    public DateOnly? CalibrationValidUntil { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Station (servis içi ilişki).</summary>
    public Station? Station { get; set; }
}
