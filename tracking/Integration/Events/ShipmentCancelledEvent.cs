using BaseForge.Core.Messaging;

namespace Tracking.Integration.Events;

/// <summary>kantar/ShipmentCancelled — tüketici sözleşmesi (yalnız kullanılan alanlar).</summary>
public sealed class ShipmentCancelledEvent : IIntegrationEvent
{
    /// <summary>RabbitMQ olay tipi.</summary>
    public const string Type = "kantar/ShipmentCancelled";

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

    /// <summary>İptal zamanı.</summary>
    public DateTimeOffset CancelledAt { get; init; }
}
