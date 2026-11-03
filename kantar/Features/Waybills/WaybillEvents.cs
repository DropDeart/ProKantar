using BaseForge.Core.Messaging;

namespace Kantar.Features.Waybills;

/// <summary>Bir Waybill oluşturulduğunda RabbitMQ'ya yayınlanan olay.</summary>
public sealed class WaybillCreatedEvent : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string EventType => "kantar/WaybillCreated";

    /// <summary>Oluşturulan Waybill kaydı.</summary>
    public required WaybillDto Data { get; init; }
}

