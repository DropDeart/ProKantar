using Platform.Entities;

namespace Platform.Features.Devices;

/// <summary>Device veri transfer nesnesi.</summary>
public sealed class DeviceDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>DeviceIdentifier.</summary>
    public string DeviceIdentifier { get; set; } = string.Empty;
    /// <summary>Platform.</summary>
    public DevicePlatform Platform { get; set; }
    /// <summary>PushToken.</summary>
    public string? PushToken { get; set; }
    /// <summary>AppVersion.</summary>
    public string? AppVersion { get; set; }
    /// <summary>LastSeenAt.</summary>
    public DateTimeOffset? LastSeenAt { get; set; }
    /// <summary>DriverId.</summary>
    public Guid DriverId { get; set; }

    /// <summary>Bir Device entity'sinden DTO üretir.</summary>
    public static DeviceDto From(Device entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new DeviceDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            DeviceIdentifier = entity.DeviceIdentifier,
            Platform = entity.Platform,
            PushToken = entity.PushToken,
            AppVersion = entity.AppVersion,
            LastSeenAt = entity.LastSeenAt,
            DriverId = entity.DriverId,
        };
    }
}
