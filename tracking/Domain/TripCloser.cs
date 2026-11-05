using Tracking.Data.Pings;
using Tracking.Entities;

namespace Tracking.Domain;

/// <summary>Durakları biten seferi kapatır, rotasını kaydeder ve kantar'a bildirir.</summary>
public interface ITripCloser
{
    /// <summary>Kapattıysa true. Değişikliği çağıran kaydeder.</summary>
    Task<bool> CloseIfDoneAsync(Trip trip, DateTimeOffset at, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ITripCloser" />
internal sealed class TripCloser(ITripRouteStore routes, ITripStatusPublisher statusPublisher) : ITripCloser
{
    public async Task<bool> CloseIfDoneAsync(Trip trip, DateTimeOffset at, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trip);
        if (!trip.CompleteIfAllStopsDone(at))
        {
            return false;
        }

        await routes.RecordAsync(trip.Id, trip.StartedAt ?? trip.PlannedAt, cancellationToken);
        await statusPublisher.PublishAsync(trip, at, cancellationToken);
        return true;
    }
}
