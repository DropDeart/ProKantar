using Platform.Entities;

namespace Platform.Features.DriverVehicles;

/// <summary>DriverVehicle veri transfer nesnesi.</summary>
public sealed class DriverVehicleDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>StartedAt.</summary>
    public DateTimeOffset StartedAt { get; set; }
    /// <summary>EndedAt.</summary>
    public DateTimeOffset? EndedAt { get; set; }
    /// <summary>DriverId.</summary>
    public Guid DriverId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
    /// <summary>TrailerId.</summary>
    public Guid? TrailerId { get; set; }

    /// <summary>Bir DriverVehicle entity'sinden DTO üretir.</summary>
    public static DriverVehicleDto From(DriverVehicle entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new DriverVehicleDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            StartedAt = entity.StartedAt,
            EndedAt = entity.EndedAt,
            DriverId = entity.DriverId,
            VehicleId = entity.VehicleId,
            TrailerId = entity.TrailerId,
        };
    }
}
