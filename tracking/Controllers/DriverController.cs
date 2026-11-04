using BaseForge.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracking.Common;
using Tracking.Features.DriverApp;
using Tracking.Features.Trips;

namespace Tracking.Controllers;

/// <summary>Şoför uygulaması uçları; kullanıcı token'daki sub'dan bulunur.</summary>
[Authorize(Roles = TrackingRoles.Driver)]
[Route("api/driver")]
public sealed class DriverController : BaseController
{
    /// <summary>Toplu konum gönderir.</summary>
    [HttpPost("pings")]
    public async Task<ActionResult<IngestPingsResult>> IngestPings(IngestPingsCommand command, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(command, cancellationToken));

    /// <summary>O anki sefer; yoksa 204.</summary>
    [HttpGet("trips/active")]
    public async Task<ActionResult<TripDto>> GetActiveTrip(CancellationToken cancellationToken)
    {
        var trip = await Mediator.Send(new GetMyActiveTripQuery(), cancellationToken);
        return trip is null ? NoContent() : Ok(trip);
    }

    /// <summary>Durağı onaylar ("Teslim ettim").</summary>
    [HttpPost("trips/{tripId:guid}/stops/{stopId:guid}/confirm")]
    public async Task<IActionResult> ConfirmStop(Guid tripId, Guid stopId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new ConfirmStopCommand { TripId = tripId, StopId = stopId }, cancellationToken);
        return NoContent();
    }
}
