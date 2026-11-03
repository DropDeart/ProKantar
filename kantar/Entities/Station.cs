using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>Station entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Station : BaseEntity, ITenantEntity
{
    /// <summary>Name.</summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Code.</summary>
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;
    /// <summary>Address.</summary>
    public string? Address { get; set; }
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
}
