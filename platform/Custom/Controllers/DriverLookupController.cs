using BaseForge.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Custom.Drivers;

namespace Platform.Custom.Controllers;

/// <summary>Şirket personeli için şoför arama (elle yazıldı; generate dokunmaz).</summary>
[Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
[Route("api/Drivers")]
public sealed class DriverLookupController : BaseController
{
    /// <summary>Telefonla birebir eşleşen aktif şoför; yoksa 404.</summary>
    [HttpGet("lookup")]
    public async Task<ActionResult<DriverLookupDto>> Lookup([FromQuery] string phone, CancellationToken cancellationToken)
        => await Mediator.Send(new DriverLookupQuery { Phone = phone }, cancellationToken) is { } driver
            ? Ok(driver)
            : NotFound();
}
