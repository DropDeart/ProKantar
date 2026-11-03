using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Platform.Entities;

/// <summary>Carrier entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Carrier : BaseEntity
{
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    /// <summary>TaxNo.</summary>
    [MaxLength(11)]
    public string? TaxNo { get; set; }
    /// <summary>TaxOffice.</summary>
    [MaxLength(100)]
    public string? TaxOffice { get; set; }
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }
    /// <summary>Address.</summary>
    public string? Address { get; set; }
}
