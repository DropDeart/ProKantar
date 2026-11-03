using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Platform.Entities;

/// <summary>DriverVehicle entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class DriverVehicle : BaseEntity
{
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
    /// <summary>Driver (servis içi ilişki).</summary>
    public Driver? Driver { get; set; }
    /// <summary>Vehicle (servis içi ilişki).</summary>
    public Vehicle? Vehicle { get; set; }
    /// <summary>Trailer (servis içi ilişki).</summary>
    public Trailer? Trailer { get; set; }
}
