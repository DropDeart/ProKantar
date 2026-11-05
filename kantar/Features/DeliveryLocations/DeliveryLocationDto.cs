using Kantar.Entities;

namespace Kantar.Features.DeliveryLocations;

/// <summary>DeliveryLocation veri transfer nesnesi.</summary>
public sealed class DeliveryLocationDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Address.</summary>
    public string? Address { get; set; }
    /// <summary>İl.</summary>
    public string? City { get; set; }
    /// <summary>İlçe.</summary>
    public string? District { get; set; }
    /// <summary>Posta kodu (5 hane).</summary>
    public string? PostalCode { get; set; }
    /// <summary>Kaynak ProKantar şirketi (nokta o şirketin istasyonundan alındıysa).</summary>
    public Guid? SourceTenantId { get; set; }
    /// <summary>Kaynak istasyon (o şirkette).</summary>
    public Guid? SourceStationId { get; set; }
    /// <summary>Latitude.</summary>
    public decimal Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 150;
    /// <summary>ContactName.</summary>
    public string? ContactName { get; set; }
    /// <summary>ContactPhone.</summary>
    public string? ContactPhone { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>CompanyId.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Bir DeliveryLocation entity'sinden DTO üretir.</summary>
    public static DeliveryLocationDto From(DeliveryLocation entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new DeliveryLocationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Address = entity.Address,
            City = entity.City,
            District = entity.District,
            PostalCode = entity.PostalCode,
            SourceTenantId = entity.SourceTenantId,
            SourceStationId = entity.SourceStationId,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            RadiusM = entity.RadiusM,
            ContactName = entity.ContactName,
            ContactPhone = entity.ContactPhone,
            IsActive = entity.IsActive,
            CompanyId = entity.CompanyId,
        };
    }
}
