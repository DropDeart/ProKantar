using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Platform.Entities;

/// <summary>Device entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Device : BaseEntity
{
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>DeviceIdentifier.</summary>
    [MaxLength(100)]
    public string DeviceIdentifier { get; set; } = string.Empty;
    /// <summary>Platform.</summary>
    public DevicePlatform Platform { get; set; }
    /// <summary>PushToken.</summary>
    [MaxLength(500)]
    public string? PushToken { get; set; }
    /// <summary>AppVersion.</summary>
    [MaxLength(20)]
    public string? AppVersion { get; set; }
    /// <summary>LastSeenAt.</summary>
    public DateTimeOffset? LastSeenAt { get; set; }
    /// <summary>DriverId.</summary>
    public Guid DriverId { get; set; }
    /// <summary>Driver (servis içi ilişki).</summary>
    public Driver? Driver { get; set; }
}
