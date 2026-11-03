using BaseForge.Core.Messaging;

namespace Kantar.Features.Weighings;

/// <summary>Bir Weighing oluşturulduğunda RabbitMQ'ya yayınlanan olay.</summary>
public sealed class WeighingCreatedEvent : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string EventType => "kantar/WeighingCreated";

    /// <summary>Oluşturulan Weighing kaydı.</summary>
    public required WeighingDto Data { get; init; }
}

/// <summary>Bir Weighing güncellendiğinde RabbitMQ'ya yayınlanan olay.</summary>
public sealed class WeighingUpdatedEvent : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string EventType => "kantar/WeighingUpdated";

    /// <summary>Güncellenen Weighing kaydı (güncel hâli).</summary>
    public required WeighingDto Data { get; init; }
}

