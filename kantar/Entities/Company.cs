using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>Company entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Company : BaseEntity, ITenantEntity
{
    /// <summary>Name.</summary>
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    /// <summary>ShortName.</summary>
    [MaxLength(50)]
    public string? ShortName { get; set; }
    /// <summary>TaxNo.</summary>
    [MaxLength(11)]
    public string? TaxNo { get; set; }
    /// <summary>TaxOffice.</summary>
    [MaxLength(100)]
    public string? TaxOffice { get; set; }
    /// <summary>IsCustomer.</summary>
    public bool IsCustomer { get; set; } = false;
    /// <summary>IsSupplier.</summary>
    public bool IsSupplier { get; set; } = false;
    /// <summary>IsCarrier.</summary>
    public bool IsCarrier { get; set; } = false;
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }
    /// <summary>Email.</summary>
    [MaxLength(150)]
    public string? Email { get; set; }
    /// <summary>Address.</summary>
    public string? Address { get; set; }
    /// <summary>Bağlı ProKantar şirketi (firma rehberinden eklendiyse).</summary>
    public Guid? LinkedTenantId { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
}
