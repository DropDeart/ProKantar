using BaseForge.Core.Messaging;
using BaseForge.Core.MultiTenancy;
using Tracking.Entities;
using Tracking.Integration.Events;

namespace Tracking.Domain;

/// <summary>Sefer durum değişikliğini kantar'a bildirir.</summary>
public interface ITripStatusPublisher
{
    /// <summary>Seferin o anki durumunu outbox'a yazar; çağıran kaydeder (aynı transaction).</summary>
    Task PublishAsync(Trip trip, DateTimeOffset at, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ITripStatusPublisher" />
internal sealed class TripStatusPublisher(IEventBus eventBus) : ITripStatusPublisher
{
    public async Task PublishAsync(Trip trip, DateTimeOffset at, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trip);

        // Şoför token'ında kiracı yok: zarf kiracısı seferden verilir, tüketici bu kiracıda çalışır.
        using var tenantScope = TenantScope.Enter(trip.TenantId);
        await eventBus.PublishAsync(new TripStatusChangedEvent
        {
            TenantId = trip.TenantId,
            TripId = trip.Id,
            Status = trip.Status,
            ChangedAt = at,
        }, cancellationToken);
    }
}
