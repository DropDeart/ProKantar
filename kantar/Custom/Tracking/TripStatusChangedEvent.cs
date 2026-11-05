using System.Text.Json.Serialization;
using BaseForge.Core.Messaging;
using BaseForge.Core.Serialization;

namespace Kantar.Custom.Tracking;

/// <summary>tracking/TripStatusChanged — tüketici sözleşmesi (yalnız kullanılan alanlar).</summary>
public sealed class TripStatusChangedEvent : IIntegrationEvent
{
    /// <summary>RabbitMQ olay tipi.</summary>
    public const string Type = "tracking/TripStatusChanged";

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; }

    /// <inheritdoc />
    public string EventType => Type;

    /// <summary>Seferin kiracısı.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Sefer (= Shipment Id).</summary>
    public Guid TripId { get; init; }

    /// <summary>Seferin yeni durumu.</summary>
    public TripProgress Status { get; init; }

    /// <summary>Durumun değiştiği an.</summary>
    public DateTimeOffset ChangedAt { get; init; }
}

/// <summary>Sefer durumu; değer adları tracking'deki TripStatus ile aynıdır.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<TripProgress>))]
public enum TripProgress
{
    /// <summary>Planlandı.</summary>
    Planned,

    /// <summary>Yolda (ilk konum geldi).</summary>
    Active,

    /// <summary>Tüm duraklar bitti.</summary>
    Completed,

    /// <summary>İptal edildi.</summary>
    Cancelled,
}
