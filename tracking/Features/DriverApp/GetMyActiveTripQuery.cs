using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Tracking.Common;
using Tracking.Features.Trips;

namespace Tracking.Features.DriverApp;

/// <summary>Şoförün o anki seferi; yoksa null.</summary>
public sealed class GetMyActiveTripQuery : IQuery<TripDto?>;

internal sealed class GetMyActiveTripHandler(ICurrentUser currentUser, IDriverTripFinder tripFinder)
    : IQueryHandler<GetMyActiveTripQuery, TripDto?>
{
    public async Task<TripDto?> Handle(GetMyActiveTripQuery request, CancellationToken cancellationToken)
    {
        var trip = await tripFinder.FindOpenTripAsync(currentUser.RequireUserId(), cancellationToken);
        return trip is null ? null : TripDto.From(trip);
    }
}
