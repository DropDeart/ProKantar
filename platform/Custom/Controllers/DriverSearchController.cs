using BaseForge.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Custom.Drivers;

namespace Platform.Custom.Controllers;

/// <summary>Şirket personeli için şoför arama (elle yazıldı; generate dokunmaz).</summary>
[Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
[Route("api/Drivers")]
public sealed class DriverSearchController : BaseController
{
    /// <summary>Telefon, plaka ya da ad soyadla aktif şoförler (en çok 10).</summary>
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<DriverSearchItemDto>>> Search(
        [FromQuery] DriverSearchBy by, [FromQuery] string q, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new DriverSearchQuery { By = by, Query = q ?? string.Empty }, cancellationToken));
}
