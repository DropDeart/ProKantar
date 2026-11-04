using BaseForge.Core.Messaging;

namespace Tracking.Integration.Events;

/// <summary>kantar/WeighingCompleted — tüketici sözleşmesi (yalnız kullanılan alanlar).</summary>
public sealed class WeighingCompletedEvent : IIntegrationEvent
{
    /// <summary>RabbitMQ olay tipi.</summary>
    public const string Type = "kantar/WeighingCompleted";

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; }

    /// <inheritdoc />
    public string EventType => Type;

    /// <summary>Tartım.</summary>
    public Guid WeighingId { get; init; }

    /// <summary>Bağlı sevkiyat (= Trip Id); yoksa null.</summary>
    public Guid? ShipmentId { get; init; }

    /// <summary>Tamamlanma zamanı.</summary>
    public DateTimeOffset CompletedAt { get; init; }
}
