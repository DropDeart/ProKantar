using Tracking.Data.Pings;
using Tracking.Entities;

namespace Tracking.Domain;

/// <summary>Durakları biten seferi kapatır ve rotasını kaydeder.</summary>
public interface ITripCloser
{
    /// <summary>Kapattıysa true. Değişikliği çağıran kaydeder.</summary>
    Task<bool> CloseIfDoneAsync(Trip trip, DateTimeOffset at, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ITripCloser" />
internal sealed class TripCloser(ITripRouteStore routes) : ITripCloser
{
    public async Task<bool> CloseIfDoneAsync(Trip trip, DateTimeOffset at, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trip);
        if (!trip.CompleteIfAllStopsDone(at))
        {
            return false;
        }

        await routes.RecordAsync(trip.Id, trip.StartedAt ?? trip.PlannedAt, cancellationToken);
        return true;
    }
}
