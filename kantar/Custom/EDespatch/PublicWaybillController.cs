using BaseForge.API.Controllers;
using Kantar.Custom.EDespatch.Issuing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kantar.Custom.EDespatch;

/// <summary>Şoföre SMS/bildirimle giden süreli irsaliye linki (giriş istemez, salt okunur).</summary>
[AllowAnonymous]
[Route("api/public")]
public sealed class PublicWaybillController : BaseController
{
    /// <summary>Linkteki irsaliye; token geçersiz ya da süresi dolmuşsa 404.</summary>
    [HttpGet("waybills/{token}")]
    public async Task<ActionResult<PublicWaybillView>> Get(string token, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new PublicWaybillQuery { Token = token }, cancellationToken));
}
