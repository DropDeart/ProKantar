using BaseForge.Core.Messaging;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>
/// İrsaliye kesildiğinde platforma giden olay: platform şoföre bildirim zincirini (SignalR → push → SMS) başlatır.
/// </summary>
public sealed class WaybillIssuedEvent : IIntegrationEvent
{
    /// <summary>Olay tipi (abonelik anahtarı).</summary>
    public const string Type = "kantar/WaybillIssued";

    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string EventType => Type;

    /// <summary>Kiracı.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>İrsaliye.</summary>
    public required Guid WaybillId { get; init; }

    /// <summary>İrsaliye numarası.</summary>
    public required string WaybillNo { get; init; }

    /// <summary>Tartım.</summary>
    public required Guid WeighingId { get; init; }

    /// <summary>Bağlı sevkiyat (yoksa null).</summary>
    public Guid? ShipmentId { get; init; }

    /// <summary>Sevkiyat numarası.</summary>
    public string? ShipmentNo { get; init; }

    /// <summary>platform/Driver kimliği; yoksa bildirim gönderilmez.</summary>
    public Guid? DriverId { get; init; }

    /// <summary>Şoför adı.</summary>
    public string? DriverName { get; init; }

    /// <summary>Araç plakası.</summary>
    public required string Plate { get; init; }

    /// <summary>Teslim adresi (tek satır).</summary>
    public string? DeliveryAddress { get; init; }

    /// <summary>Giriş istemeyen süreli irsaliye linki.</summary>
    public required string LinkUrl { get; init; }

    /// <summary>Düzenleme anı.</summary>
    public required DateTimeOffset IssuedAt { get; init; }
}
