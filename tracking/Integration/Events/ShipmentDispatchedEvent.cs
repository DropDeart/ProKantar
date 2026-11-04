using BaseForge.Core.Messaging;
using Tracking.Entities;

namespace Tracking.Integration.Events;

/// <summary>
/// kantar/ShipmentDispatched — tüketici tarafı sözleşmesi. Alan adları kantar'daki olayla aynı;
/// enum değerleri adla taşınır (TripDirection/StopType kantar'daki adlarla birebir).
/// </summary>
public sealed class ShipmentDispatchedEvent : IIntegrationEvent
{
    /// <summary>RabbitMQ olay tipi.</summary>
    public const string Type = "kantar/ShipmentDispatched";

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; }

    /// <inheritdoc />
    public string EventType => Type;

    /// <summary>Kiracı.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Sevkiyat (= Trip Id).</summary>
    public Guid ShipmentId { get; init; }

    /// <summary>Sevkiyat numarası.</summary>
    public string ShipmentNo { get; init; } = string.Empty;

    /// <summary>Gelen / giden.</summary>
    public TripDirection Direction { get; init; }

    /// <summary>Planlanan zaman.</summary>
    public DateTimeOffset PlannedAt { get; init; }

    /// <summary>İstasyon.</summary>
    public Guid StationId { get; init; }

    /// <summary>platform/Vehicle.</summary>
    public Guid VehicleId { get; init; }

    /// <summary>Plaka.</summary>
    public string Plate { get; init; } = string.Empty;

    /// <summary>platform/Driver.</summary>
    public Guid DriverId { get; init; }

    /// <summary>Şoför adı.</summary>
    public string? DriverName { get; init; }

    /// <summary>Duraklar.</summary>
    public List<ShipmentStopPayload> Stops { get; init; } = [];
}

/// <summary>Olaydaki durak.</summary>
public sealed record ShipmentStopPayload(
    Guid ShipmentStopId,
    int Sequence,
    StopType StopType,
    string Name,
    decimal Latitude,
    decimal Longitude,
    int RadiusM,
    DateTimeOffset? PlannedAt,
    Guid? WaybillId);
