using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>Weighing entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Weighing : BaseEntity, ITenantEntity
{
    /// <summary>TicketNo.</summary>
    [MaxLength(30)]
    public string TicketNo { get; set; } = string.Empty;
    /// <summary>Direction.</summary>
    public WeighingDirection Direction { get; set; }
    /// <summary>Status.</summary>
    public WeighingStatus Status { get; set; } = WeighingStatus.Open;
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>TrailerId.</summary>
    public Guid? TrailerId { get; set; }
    /// <summary>TrailerPlate.</summary>
    [MaxLength(15)]
    public string? TrailerPlate { get; set; }
    /// <summary>DriverId.</summary>
    public Guid? DriverId { get; set; }
    /// <summary>DriverName.</summary>
    [MaxLength(150)]
    public string? DriverName { get; set; }
    /// <summary>DriverPhone.</summary>
    [MaxLength(20)]
    public string? DriverPhone { get; set; }
    /// <summary>FirstWeightKg.</summary>
    public decimal FirstWeightKg { get; set; }
    /// <summary>FirstWeighedAt.</summary>
    public DateTimeOffset FirstWeighedAt { get; set; }
    /// <summary>FirstIsManual.</summary>
    public bool FirstIsManual { get; set; } = false;
    /// <summary>SecondWeighbridgeId.</summary>
    public Guid? SecondWeighbridgeId { get; set; }
    /// <summary>SecondWeightKg.</summary>
    public decimal? SecondWeightKg { get; set; }
    /// <summary>SecondWeighedAt.</summary>
    public DateTimeOffset? SecondWeighedAt { get; set; }
    /// <summary>SecondIsManual.</summary>
    public bool SecondIsManual { get; set; } = false;
    /// <summary>GrossKg.</summary>
    public decimal? GrossKg { get; set; }
    /// <summary>TareKg.</summary>
    public decimal? TareKg { get; set; }
    /// <summary>NetKg.</summary>
    public decimal? NetKg { get; set; }
    /// <summary>TareSource.</summary>
    public WeighingTareSource? TareSource { get; set; }
    /// <summary>CancelReason.</summary>
    [MaxLength(250)]
    public string? CancelReason { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
    /// <summary>WeighbridgeId.</summary>
    public Guid WeighbridgeId { get; set; }
    /// <summary>CompanyId.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>MaterialId.</summary>
    public Guid MaterialId { get; set; }
    /// <summary>ShipmentId.</summary>
    public Guid? ShipmentId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Station (servis içi ilişki).</summary>
    public Station? Station { get; set; }
    /// <summary>Weighbridge (servis içi ilişki).</summary>
    public Weighbridge? Weighbridge { get; set; }
    /// <summary>Company (servis içi ilişki).</summary>
    public Company? Company { get; set; }
    /// <summary>Material (servis içi ilişki).</summary>
    public Material? Material { get; set; }
    /// <summary>Shipment (servis içi ilişki).</summary>
    public Shipment? Shipment { get; set; }
}
