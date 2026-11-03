using BaseForge.Core.Messaging;
using Kantar.Entities;

namespace Kantar.Custom.Shipments;

/// <summary>
/// Sevkiyat planı yayınlandığında (veya yeniden yayınlandığında) tracking'e giden olay.
/// BaseForge CRUD olaylarından farklı olarak <see cref="TenantId"/> taşır; tracking seferi buna göre açar.
/// </summary>
public sealed class ShipmentDispatchedEvent : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string EventType => "kantar/ShipmentDispatched";

    /// <summary>Sevkiyatın ait olduğu kiracı.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Sevkiyat kimliği (tracking'de Trip kimliği olarak da kullanılır).</summary>
    public required Guid ShipmentId { get; init; }

    /// <summary>Sevkiyat numarası.</summary>
    public required string ShipmentNo { get; init; }

    /// <summary>Gelen (randevu) veya giden sevkiyat.</summary>
    public required ShipmentDirection Direction { get; init; }

    /// <summary>Planlanan zaman.</summary>
    public required DateTimeOffset PlannedAt { get; init; }

    /// <summary>Sevkiyatın bağlı olduğu istasyon.</summary>
    public required Guid StationId { get; init; }

    /// <summary>platform/Vehicle kimliği.</summary>
    public required Guid VehicleId { get; init; }

    /// <summary>Plaka.</summary>
    public required string Plate { get; init; }

    /// <summary>platform/Driver kimliği; seferi şoförün uygulamasına bu bağlar.</summary>
    public required Guid DriverId { get; init; }

    /// <summary>Şoför adı.</summary>
    public string? DriverName { get; init; }

    /// <summary>Sıraya göre duraklar.</summary>
    public required IReadOnlyList<ShipmentStopPayload> Stops { get; init; }
}

/// <summary>
/// <see cref="ShipmentDispatchedEvent"/> içindeki tek bir durak. Generate edilen <c>ShipmentStopDto</c> yerine
/// bilinçli olarak dar tutulmuş sabit sözleşme: tracking yalnızca bu alanlara bağımlıdır, entity'ye eklenen
/// alanlar sözleşmeyi kendiliğinden değiştirmez.
/// </summary>
public sealed record ShipmentStopPayload(
    Guid ShipmentStopId,
    int Sequence,
    ShipmentStopStopType StopType,
    string Name,
    decimal Latitude,
    decimal Longitude,
    int RadiusM,
    DateTimeOffset? PlannedAt,
    Guid? WaybillId)
{
    /// <summary>Entity'den durak yükü üretir.</summary>
    public static ShipmentStopPayload From(ShipmentStop stop)
    {
        ArgumentNullException.ThrowIfNull(stop);
        return new(stop.Id, stop.Sequence, stop.StopType, stop.Name, stop.Latitude, stop.Longitude,
            stop.RadiusM, stop.PlannedAt, stop.WaybillId);
    }
}
