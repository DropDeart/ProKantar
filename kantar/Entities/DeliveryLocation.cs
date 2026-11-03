using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>DeliveryLocation entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class DeliveryLocation : BaseEntity, ITenantEntity
{
    /// <summary>Name.</summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Address.</summary>
    public string? Address { get; set; }
    /// <summary>Latitude.</summary>
    public decimal Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 150;
    /// <summary>ContactName.</summary>
    [MaxLength(100)]
    public string? ContactName { get; set; }
    /// <summary>ContactPhone.</summary>
    [MaxLength(20)]
    public string? ContactPhone { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>CompanyId.</summary>
    public Guid CompanyId { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Company (servis içi ilişki).</summary>
    public Company? Company { get; set; }
}
