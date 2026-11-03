using Kantar.Entities;

namespace Kantar.Features.ShipmentStops;

/// <summary>ShipmentStop veri transfer nesnesi.</summary>
public sealed class ShipmentStopDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Sequence.</summary>
    public int Sequence { get; set; }
    /// <summary>StopType.</summary>
    public ShipmentStopStopType StopType { get; set; }
    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Latitude.</summary>
    public decimal Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 150;
    /// <summary>PlannedAt.</summary>
    public DateTimeOffset? PlannedAt { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>ShipmentId.</summary>
    public Guid ShipmentId { get; set; }
    /// <summary>DeliveryLocationId.</summary>
    public Guid? DeliveryLocationId { get; set; }
    /// <summary>StationId.</summary>
    public Guid? StationId { get; set; }
    /// <summary>WaybillId.</summary>
    public Guid? WaybillId { get; set; }

    /// <summary>Bir ShipmentStop entity'sinden DTO üretir.</summary>
    public static ShipmentStopDto From(ShipmentStop entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new ShipmentStopDto
        {
            Id = entity.Id,
            Sequence = entity.Sequence,
            StopType = entity.StopType,
            Name = entity.Name,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            RadiusM = entity.RadiusM,
            PlannedAt = entity.PlannedAt,
            Note = entity.Note,
            ShipmentId = entity.ShipmentId,
            DeliveryLocationId = entity.DeliveryLocationId,
            StationId = entity.StationId,
            WaybillId = entity.WaybillId,
        };
    }
}
