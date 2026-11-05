using Kantar.Entities;

namespace Kantar.Features.Stations;

/// <summary>Station veri transfer nesnesi.</summary>
public sealed class StationDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Address.</summary>
    public string? Address { get; set; }
    /// <summary>İl.</summary>
    public string? City { get; set; }
    /// <summary>İlçe.</summary>
    public string? District { get; set; }
    /// <summary>Posta kodu (5 hane).</summary>
    public string? PostalCode { get; set; }
    /// <summary>Phone.</summary>
    public string? Phone { get; set; }
    /// <summary>Latitude.</summary>
    public decimal? Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal? Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 300;
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Bir Station entity'sinden DTO üretir.</summary>
    public static StationDto From(Station entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new StationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            Address = entity.Address,
            City = entity.City,
            District = entity.District,
            PostalCode = entity.PostalCode,
            Phone = entity.Phone,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            RadiusM = entity.RadiusM,
            IsActive = entity.IsActive,
        };
    }
}
