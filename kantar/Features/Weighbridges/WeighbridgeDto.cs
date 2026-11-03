using Kantar.Entities;

namespace Kantar.Features.Weighbridges;

/// <summary>Weighbridge veri transfer nesnesi.</summary>
public sealed class WeighbridgeDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>SerialNo.</summary>
    public string? SerialNo { get; set; }
    /// <summary>CapacityKg.</summary>
    public decimal CapacityKg { get; set; }
    /// <summary>ConnectionType.</summary>
    public WeighbridgeConnectionType? ConnectionType { get; set; }
    /// <summary>ConnectionInfo.</summary>
    public string? ConnectionInfo { get; set; }
    /// <summary>CalibrationValidUntil.</summary>
    public DateOnly? CalibrationValidUntil { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }

    /// <summary>Bir Weighbridge entity'sinden DTO üretir.</summary>
    public static WeighbridgeDto From(Weighbridge entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new WeighbridgeDto
        {
            Id = entity.Id,
            Name = entity.Name,
            SerialNo = entity.SerialNo,
            CapacityKg = entity.CapacityKg,
            ConnectionType = entity.ConnectionType,
            ConnectionInfo = entity.ConnectionInfo,
            CalibrationValidUntil = entity.CalibrationValidUntil,
            IsActive = entity.IsActive,
            StationId = entity.StationId,
        };
    }
}
