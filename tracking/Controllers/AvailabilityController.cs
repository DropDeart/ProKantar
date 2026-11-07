using BaseForge.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracking.Common;
using Tracking.Features.Availability;

namespace Tracking.Controllers;

/// <summary>Şoförün "Müsaitim" modu (mobil).</summary>
[Authorize(Roles = TrackingRoles.Driver)]
[Route("api/driver/availability")]
public sealed class DriverAvailabilityController : BaseController
{
    /// <summary>Müsait modunu açar / konumu yeniler (mod açıkken birkaç dakikada bir; 30 dk gelmezse düşer).</summary>
    [HttpPut]
    public async Task<IActionResult> SetAvailable(SetAvailableCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Müsait modunu kapatır; konum silinir.</summary>
    [HttpDelete]
    public async Task<IActionResult> SetUnavailable(CancellationToken cancellationToken)
    {
        await Mediator.Send(new SetUnavailableCommand(), cancellationToken);
        return NoContent();
    }
}

/// <summary>Firma: yakındaki müsait şoförler (yalnız kullanıcı kimliği ve yuvarlanmış mesafe).</summary>
[Authorize(Roles = TrackingRoles.Staff)]
[Route("api/availability")]
public sealed class AvailabilityController : BaseController
{
    /// <summary>Noktaya yakın müsait şoförler, yakından uzağa (en çok 50).</summary>
    [HttpGet("nearby")]
    public async Task<ActionResult<IReadOnlyList<NearbyDriver>>> Nearby([FromQuery] NearbyDriversQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));
}
