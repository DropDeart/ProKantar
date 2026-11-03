using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>Machine entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Machine : BaseEntity, ITenantEntity
{
    /// <summary>Name.</summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    /// <summary>MachineType.</summary>
    [MaxLength(50)]
    public string MachineType { get; set; } = string.Empty;
    /// <summary>PlateOrSerial.</summary>
    [MaxLength(50)]
    public string? PlateOrSerial { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Station (servis içi ilişki).</summary>
    public Station? Station { get; set; }
}
