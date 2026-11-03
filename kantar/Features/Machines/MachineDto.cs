using Kantar.Entities;

namespace Kantar.Features.Machines;

/// <summary>Machine veri transfer nesnesi.</summary>
public sealed class MachineDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>MachineType.</summary>
    public string MachineType { get; set; } = string.Empty;
    /// <summary>PlateOrSerial.</summary>
    public string? PlateOrSerial { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }

    /// <summary>Bir Machine entity'sinden DTO üretir.</summary>
    public static MachineDto From(Machine entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new MachineDto
        {
            Id = entity.Id,
            Name = entity.Name,
            MachineType = entity.MachineType,
            PlateOrSerial = entity.PlateOrSerial,
            IsActive = entity.IsActive,
            StationId = entity.StationId,
        };
    }
}
