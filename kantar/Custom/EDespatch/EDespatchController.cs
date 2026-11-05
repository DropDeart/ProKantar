using BaseForge.API.Controllers;
using Kantar.Custom.EDespatch.Providers;
using Kantar.Custom.EDespatch.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kantar.Custom.EDespatch;

/// <summary>Kiracının e-İrsaliye yöntemi ve entegratör ayarı (yalnız kiracı yöneticisi).</summary>
[Authorize(Roles = "TenantAdmin")]
[Route("api/edespatch")]
public sealed class EDespatchController : BaseController
{
    /// <summary>Kayıtlı ayar; şifre dönmez, yalnız kayıtlı olup olmadığı.</summary>
    [HttpGet("settings")]
    public async Task<ActionResult<EDespatchSettingsDto>> GetSettings(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetEDespatchSettingsQuery(), cancellationToken));

    /// <summary>Ayarı kaydeder; şifre boşsa kayıtlı şifre korunur.</summary>
    [HttpPut("settings")]
    public async Task<IActionResult> SaveSettings(SaveEDespatchSettingsCommand command, CancellationToken cancellationToken)
    {
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Seçili sağlayıcıyla bağlantı testi (e-Logo: Login + Logout).</summary>
    [HttpPost("settings/test")]
    public async Task<ActionResult<ConnectionTestResult>> TestConnection(TestEDespatchConnectionCommand? command, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(command ?? new TestEDespatchConnectionCommand(), cancellationToken));
}
