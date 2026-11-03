using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Platform.Entities;

/// <summary>Vehicle entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Vehicle : BaseEntity
{
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>VehicleTypeCode.</summary>
    [MaxLength(30)]
    public string? VehicleTypeCode { get; set; }
    /// <summary>Brand.</summary>
    [MaxLength(50)]
    public string? Brand { get; set; }
    /// <summary>Model.</summary>
    [MaxLength(50)]
    public string? Model { get; set; }
    /// <summary>IsClaimed.</summary>
    public bool IsClaimed { get; set; } = false;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }
    /// <summary>Carrier (servis içi ilişki).</summary>
    public Carrier? Carrier { get; set; }
}
