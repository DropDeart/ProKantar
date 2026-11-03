using Platform.Entities;

namespace Platform.Features.Drivers;

/// <summary>Driver veri transfer nesnesi.</summary>
public sealed class DriverDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>FullName.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Phone.</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>NationalId.</summary>
    public string? NationalId { get; set; }
    /// <summary>LicenseNo.</summary>
    public string? LicenseNo { get; set; }
    /// <summary>KvkkConsentAt.</summary>
    public DateTimeOffset? KvkkConsentAt { get; set; }
    /// <summary>LocationConsent.</summary>
    public bool LocationConsent { get; set; } = false;
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }

    /// <summary>Bir Driver entity'sinden DTO üretir.</summary>
    public static DriverDto From(Driver entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new DriverDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FullName = entity.FullName,
            Phone = entity.Phone,
            NationalId = entity.NationalId,
            LicenseNo = entity.LicenseNo,
            KvkkConsentAt = entity.KvkkConsentAt,
            LocationConsent = entity.LocationConsent,
            IsActive = entity.IsActive,
            CarrierId = entity.CarrierId,
        };
    }
}
