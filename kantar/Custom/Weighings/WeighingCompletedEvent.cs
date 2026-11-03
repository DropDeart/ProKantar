using BaseForge.Core.Messaging;
using Kantar.Entities;

namespace Kantar.Custom.Weighings;

/// <summary>
/// Tartım tamamlandığında tracking'e giden olay: şoför uygulamasında "ne, kime, ne kadar" bilgisini besler.
/// BaseForge CRUD olaylarından farklı olarak <see cref="TenantId"/> ve okunur adlar taşır.
/// </summary>
public sealed class WeighingCompletedEvent : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string EventType => "kantar/WeighingCompleted";

    /// <summary>Kiracı.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Tartım kimliği.</summary>
    public required Guid WeighingId { get; init; }

    /// <summary>Bağlı sevkiyat (yoksa null).</summary>
    public Guid? ShipmentId { get; init; }

    /// <summary>Fiş numarası.</summary>
    public required string TicketNo { get; init; }

    /// <summary>Giriş (alış) / çıkış (satış).</summary>
    public required WeighingDirection Direction { get; init; }

    /// <summary>İstasyon.</summary>
    public required Guid StationId { get; init; }

    /// <summary>platform/Vehicle.</summary>
    public required Guid VehicleId { get; init; }

    /// <summary>Plaka.</summary>
    public required string Plate { get; init; }

    /// <summary>platform/Driver (kayıtsız şoförde null).</summary>
    public Guid? DriverId { get; init; }

    /// <summary>Malzeme.</summary>
    public required Guid MaterialId { get; init; }

    /// <summary>Malzeme adı.</summary>
    public required string MaterialName { get; init; }

    /// <summary>Firma (peşin satışta null).</summary>
    public Guid? CompanyId { get; init; }

    /// <summary>Firma adı.</summary>
    public string? CompanyName { get; init; }

    /// <summary>Net ağırlık (kg).</summary>
    public required decimal NetKg { get; init; }

    /// <summary>Tamamlanma zamanı.</summary>
    public required DateTimeOffset CompletedAt { get; init; }
}
