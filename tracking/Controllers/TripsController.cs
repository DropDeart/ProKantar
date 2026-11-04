using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracking.Common;
using Tracking.Entities;
using Tracking.Features.Trips;

namespace Tracking.Controllers;

/// <summary>Kiracı personeli için sefer izleme uçları.</summary>
[Authorize(Roles = TrackingRoles.Staff)]
[Route("api/Trips")]
public sealed class TripsController : BaseController
{
    /// <summary>Seferleri sayfalı listeler (page, pageSize, search, status, driverId, vehicleId, plannedFrom, plannedTo).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TripListItemDto>>> List([FromQuery] GetTripsQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Sefer ve durakları.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetTripByIdQuery { Id = id }, cancellationToken)
              ?? throw new NotFoundException(nameof(Trip), id));

    /// <summary>Harita izi (GeoJSON rota + son konum).</summary>
    [HttpGet("{id:guid}/track")]
    public async Task<ActionResult<TripTrackDto>> GetTrack(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetTripTrackQuery { Id = id }, cancellationToken)
              ?? throw new NotFoundException(nameof(Trip), id));
}
