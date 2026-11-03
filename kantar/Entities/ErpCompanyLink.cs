using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>ErpCompanyLink entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class ErpCompanyLink : BaseEntity, ITenantEntity
{
    /// <summary>ErpSystem.</summary>
    [MaxLength(30)]
    public string ErpSystem { get; set; } = string.Empty;
    /// <summary>ErpCode.</summary>
    [MaxLength(50)]
    public string ErpCode { get; set; } = string.Empty;
    /// <summary>IsDefault.</summary>
    public bool IsDefault { get; set; } = true;
    /// <summary>CompanyId.</summary>
    public Guid CompanyId { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Company (servis içi ilişki).</summary>
    public Company? Company { get; set; }
}
