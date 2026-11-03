using Kantar.Entities;

namespace Kantar.Features.VehicleTypes;

/// <summary>VehicleType veri transfer nesnesi.</summary>
public sealed class VehicleTypeDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>MaxGrossKg.</summary>
    public decimal? MaxGrossKg { get; set; }

    /// <summary>Bir VehicleType entity'sinden DTO üretir.</summary>
    public static VehicleTypeDto From(VehicleType entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new VehicleTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            MaxGrossKg = entity.MaxGrossKg,
        };
    }
}
