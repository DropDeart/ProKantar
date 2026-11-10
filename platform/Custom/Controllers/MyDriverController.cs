using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Custom.Drivers;

namespace Platform.Custom.Controllers;

/// <summary>Şoförün kendi profili (web ve mobil): özet ve kimlik (TCKN) doğrulaması.</summary>
[Authorize(Roles = "Driver")]
[Route("api/me/driver")]
public sealed class MyDriverController : CallerController
{
    /// <summary>Profil özeti (TCKN maskeli); profil yoksa 404.</summary>
    [HttpGet]
    public async Task<ActionResult<MyDriverDto>> Get(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new MyDriverQuery { UserId = UserId }, cancellationToken));

    /// <summary>TCKN'yi NVİ ile (ad, soyad, doğum yılı) doğrulayıp kaydeder; irsaliyelere buradan gelir.</summary>
    [HttpPut("national-id")]
    public async Task<ActionResult<MyDriverDto>> SetNationalId(SetMyNationalIdCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.UserId = UserId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }
}
