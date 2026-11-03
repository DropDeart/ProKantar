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
    /// <summary>Phone.</summary>
    public string? Phone { get; set; }
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
            Phone = entity.Phone,
            IsActive = entity.IsActive,
        };
    }
}
