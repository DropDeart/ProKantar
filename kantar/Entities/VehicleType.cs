using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>VehicleType entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class VehicleType : BaseEntity, ITenantEntity
{
    /// <summary>Code.</summary>
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;
    /// <summary>Name.</summary>
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    /// <summary>MaxGrossKg.</summary>
    public decimal? MaxGrossKg { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
}
