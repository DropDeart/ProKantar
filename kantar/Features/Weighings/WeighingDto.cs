using Kantar.Entities;

namespace Kantar.Features.Weighings;

/// <summary>Weighing veri transfer nesnesi.</summary>
public sealed class WeighingDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>TicketNo.</summary>
    public string TicketNo { get; set; } = string.Empty;
    /// <summary>Direction.</summary>
    public WeighingDirection Direction { get; set; }
    /// <summary>Status.</summary>
    public WeighingStatus Status { get; set; } = WeighingStatus.Open;
    /// <summary>Plate.</summary>
    public string Plate { get; set; } = string.Empty;
    /// <summary>TrailerId.</summary>
    public Guid? TrailerId { get; set; }
    /// <summary>TrailerPlate.</summary>
    public string? TrailerPlate { get; set; }
    /// <summary>DriverId.</summary>
    public Guid? DriverId { get; set; }
    /// <summary>DriverName.</summary>
    public string? DriverName { get; set; }
    /// <summary>DriverPhone.</summary>
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

    /// <summary>Bir Weighing entity'sinden DTO üretir.</summary>
    public static WeighingDto From(Weighing entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new WeighingDto
        {
            Id = entity.Id,
            TicketNo = entity.TicketNo,
            Direction = entity.Direction,
            Status = entity.Status,
            Plate = entity.Plate,
            TrailerId = entity.TrailerId,
            TrailerPlate = entity.TrailerPlate,
            DriverId = entity.DriverId,
            DriverName = entity.DriverName,
            DriverPhone = entity.DriverPhone,
            FirstWeightKg = entity.FirstWeightKg,
            FirstWeighedAt = entity.FirstWeighedAt,
            FirstIsManual = entity.FirstIsManual,
            SecondWeighbridgeId = entity.SecondWeighbridgeId,
            SecondWeightKg = entity.SecondWeightKg,
            SecondWeighedAt = entity.SecondWeighedAt,
            SecondIsManual = entity.SecondIsManual,
            GrossKg = entity.GrossKg,
            TareKg = entity.TareKg,
            NetKg = entity.NetKg,
            TareSource = entity.TareSource,
            CancelReason = entity.CancelReason,
            Note = entity.Note,
            StationId = entity.StationId,
            WeighbridgeId = entity.WeighbridgeId,
            CompanyId = entity.CompanyId,
            MaterialId = entity.MaterialId,
            ShipmentId = entity.ShipmentId,
            VehicleId = entity.VehicleId,
        };
    }
}
