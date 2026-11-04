using Microsoft.EntityFrameworkCore;
using Tracking.Data;
using Tracking.Entities;

namespace Tracking.Features.DriverApp;

/// <summary>Şoförün o anki seferini bulur.</summary>
public interface IDriverTripFinder
{
    /// <summary>Yoldaki sefer; yoksa en yakın planlı sefer (duraklarıyla, takipli).</summary>
    Task<Trip?> FindOpenTripAsync(Guid driverUserId, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IDriverTripFinder" />
internal sealed class DriverTripFinder(TrackingDbContext db) : IDriverTripFinder
{
    public Task<Trip?> FindOpenTripAsync(Guid driverUserId, CancellationToken cancellationToken)
        => db.Trips
            .Include(t => t.Stops)
            .Where(t => t.DriverUserId == driverUserId
                        && (t.Status == TripStatus.Active || t.Status == TripStatus.Planned))
            .OrderBy(t => t.Status == TripStatus.Active ? 0 : 1)
            .ThenBy(t => t.PlannedAt)
            .FirstOrDefaultAsync(cancellationToken);
}
