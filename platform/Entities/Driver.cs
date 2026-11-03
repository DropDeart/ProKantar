using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Platform.Entities;

/// <summary>Driver entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Driver : BaseEntity
{
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>FullName.</summary>
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    /// <summary>NationalId.</summary>
    [MaxLength(11)]
    public string? NationalId { get; set; }
    /// <summary>LicenseNo.</summary>
    [MaxLength(30)]
    public string? LicenseNo { get; set; }
    /// <summary>KvkkConsentAt.</summary>
    public DateTimeOffset? KvkkConsentAt { get; set; }
    /// <summary>LocationConsent.</summary>
    public bool LocationConsent { get; set; } = false;
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }
    /// <summary>Carrier (servis içi ilişki).</summary>
    public Carrier? Carrier { get; set; }
}
