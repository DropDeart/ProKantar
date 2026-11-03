using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>Material entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Material : BaseEntity, ITenantEntity
{
    /// <summary>Code.</summary>
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    /// <summary>Name.</summary>
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Unit.</summary>
    public MaterialUnit Unit { get; set; }
    /// <summary>KgPerUnit.</summary>
    public decimal? KgPerUnit { get; set; }
    /// <summary>ErpStockCode.</summary>
    [MaxLength(50)]
    public string? ErpStockCode { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
}
