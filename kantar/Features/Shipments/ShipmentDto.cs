using Kantar.Entities;

namespace Kantar.Features.Shipments;

/// <summary>Shipment veri transfer nesnesi.</summary>
public sealed class ShipmentDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>ShipmentNo.</summary>
    public string ShipmentNo { get; set; } = string.Empty;
    /// <summary>Direction.</summary>
    public ShipmentDirection Direction { get; set; }
    /// <summary>Status.</summary>
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Draft;
    /// <summary>PlannedAt.</summary>
    public DateTimeOffset PlannedAt { get; set; }
    /// <summary>Plate.</summary>
    public string Plate { get; set; } = string.Empty;
    /// <summary>DriverId.</summary>
    public Guid? DriverId { get; set; }
    /// <summary>DriverName.</summary>
    public string? DriverName { get; set; }
    /// <summary>DispatchedAt.</summary>
    public DateTimeOffset? DispatchedAt { get; set; }
    /// <summary>CancelReason.</summary>
    public string? CancelReason { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Bir Shipment entity'sinden DTO üretir.</summary>
    public static ShipmentDto From(Shipment entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new ShipmentDto
        {
            Id = entity.Id,
            ShipmentNo = entity.ShipmentNo,
            Direction = entity.Direction,
            Status = entity.Status,
            PlannedAt = entity.PlannedAt,
            Plate = entity.Plate,
            DriverId = entity.DriverId,
            DriverName = entity.DriverName,
            DispatchedAt = entity.DispatchedAt,
            CancelReason = entity.CancelReason,
            Note = entity.Note,
            StationId = entity.StationId,
            VehicleId = entity.VehicleId,
        };
    }
}
