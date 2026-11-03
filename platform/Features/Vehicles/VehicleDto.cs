using Platform.Entities;

namespace Platform.Features.Vehicles;

/// <summary>Vehicle veri transfer nesnesi.</summary>
public sealed class VehicleDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Plate.</summary>
    public string Plate { get; set; } = string.Empty;
    /// <summary>VehicleTypeCode.</summary>
    public string? VehicleTypeCode { get; set; }
    /// <summary>Brand.</summary>
    public string? Brand { get; set; }
    /// <summary>Model.</summary>
    public string? Model { get; set; }
    /// <summary>IsClaimed.</summary>
    public bool IsClaimed { get; set; } = false;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }

    /// <summary>Bir Vehicle entity'sinden DTO üretir.</summary>
    public static VehicleDto From(Vehicle entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new VehicleDto
        {
            Id = entity.Id,
            OwnerUserId = entity.OwnerUserId,
            Plate = entity.Plate,
            VehicleTypeCode = entity.VehicleTypeCode,
            Brand = entity.Brand,
            Model = entity.Model,
            IsClaimed = entity.IsClaimed,
            CarrierId = entity.CarrierId,
        };
    }
}
