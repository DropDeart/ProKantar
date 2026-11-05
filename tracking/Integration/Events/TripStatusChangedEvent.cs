using BaseForge.Core.Messaging;
using Tracking.Entities;

namespace Tracking.Integration.Events;

/// <summary>
/// Sefer yola çıktı (<see cref="TripStatus.Active"/>) ya da bitti (<see cref="TripStatus.Completed"/>);
/// kantar sevkiyat durumunu buna göre ilerletir.
/// </summary>
public sealed class TripStatusChangedEvent : IIntegrationEvent
{
    /// <summary>RabbitMQ olay tipi.</summary>
    public const string Type = "tracking/TripStatusChanged";

    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string EventType => Type;

    /// <summary>Seferin kiracısı.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Sefer (= kantar Shipment Id).</summary>
    public required Guid TripId { get; init; }

    /// <summary>Yeni durum.</summary>
    public required TripStatus Status { get; init; }

    /// <summary>Durumun değiştiği an.</summary>
    public required DateTimeOffset ChangedAt { get; init; }
}
