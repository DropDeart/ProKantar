using BaseForge.Core.Messaging;

namespace Platform.Custom.Notifications;

/// <summary>kantar/WaybillIssued — tüketici sözleşmesi (yalnız kullanılan alanlar).</summary>
public sealed class WaybillIssuedEvent : IIntegrationEvent
{
    /// <summary>RabbitMQ olay tipi.</summary>
    public const string Type = "kantar/WaybillIssued";

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; }

    /// <inheritdoc />
    public string EventType => Type;

    /// <summary>Kiracı.</summary>
    public Guid TenantId { get; init; }

    /// <summary>İrsaliye.</summary>
    public Guid WaybillId { get; init; }

    /// <summary>İrsaliye numarası.</summary>
    public string WaybillNo { get; init; } = string.Empty;

    /// <summary>Sevkiyat.</summary>
    public Guid? ShipmentId { get; init; }

    /// <summary>Sevkiyat numarası.</summary>
    public string? ShipmentNo { get; init; }

    /// <summary>platform/Driver kimliği.</summary>
    public Guid? DriverId { get; init; }

    /// <summary>Araç plakası.</summary>
    public string Plate { get; init; } = string.Empty;

    /// <summary>Teslim adresi.</summary>
    public string? DeliveryAddress { get; init; }

    /// <summary>Süreli irsaliye linki.</summary>
    public string LinkUrl { get; init; } = string.Empty;

    /// <summary>Düzenleme anı.</summary>
    public DateTimeOffset IssuedAt { get; init; }
}
