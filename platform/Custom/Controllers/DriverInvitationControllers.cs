using BaseForge.Core.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Custom.Drivers;

namespace Platform.Custom.Controllers;

/// <summary>Firmanın şoförleri ve davetleri — "Şoförlerimiz" (elle yazıldı; generate dokunmaz).</summary>
[Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
[Route("api/TenantDrivers")]
public sealed class TenantDriversController : CallerController
{
    /// <summary>Firmanın şoförleri ve bekleyen davetleri.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantDriverDto>>> List(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new TenantDriversQuery { TenantId = TenantId }, cancellationToken));

    /// <summary>Firmanın şoförleri, sayfalı: <c>?page=&amp;pageSize=&amp;search=&amp;status=</c> (liste ekranı; seçiciler tam listeyi kullanır).</summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<TenantDriverDto>>> Paged([FromQuery] TenantDriversPageQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        query.TenantId = TenantId;
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Kayıtlı şoförü ya da telefon numarasını davet eder; şoföre bildirim / SMS gider.</summary>
    [HttpPost("invite")]
    public async Task<ActionResult<Guid>> Invite(InviteDriverCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.TenantId = TenantId;
        command.TenantName = TenantName;
        command.UserId = UserId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>Daveti geri çeker ya da şoförü firmadan çıkarır.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RemoveTenantDriverCommand { Id = id, TenantId = TenantId }, cancellationToken);
        return NoContent();
    }

    /// <summary>Yakındaki müsait şoförlerin (tracking kullanıcı kimlikleri) özeti: ad, araç, firmayla bağ durumu.</summary>
    [HttpPost("summaries")]
    public async Task<ActionResult<IReadOnlyList<DriverSummaryDto>>> Summaries(DriverSummariesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        query.TenantId = TenantId;
        return Ok(await Mediator.Send(query, cancellationToken));
    }
}

/// <summary>Davet linki (<c>/davet/{kod}</c>): özet herkese açık, yanıt için oturum ve doğrulanmış numara gerekir.</summary>
[Authorize]
[Route("api/invitations")]
public sealed class InvitationLinksController : CallerController
{
    /// <summary>Davetin özeti: firma, maskeli numara, durum.</summary>
    [HttpGet("{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicInvitationDto>> Get(string token, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new InvitationByTokenQuery { Token = token }, cancellationToken));

    /// <summary>Daveti kabul eder; şoför profili yoksa adla açılır.</summary>
    [HttpPost("{token}/accept")]
    public Task<IActionResult> Accept(string token, RespondInvitationByTokenCommand command, CancellationToken cancellationToken)
        => Respond(token, true, command, cancellationToken);

    /// <summary>Daveti reddeder.</summary>
    [HttpPost("{token}/decline")]
    public Task<IActionResult> Decline(string token, CancellationToken cancellationToken)
        => Respond(token, false, new RespondInvitationByTokenCommand(), cancellationToken);

    private async Task<IActionResult> Respond(string token, bool accept, RespondInvitationByTokenCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Token = token;
        command.Accept = accept;
        command.UserId = UserId;
        command.VerifiedPhone = VerifiedPhone;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

/// <summary>Şoförün firma davetleri (mobil).</summary>
[Authorize(Roles = "Driver")]
[Route("api/me/invitations")]
public sealed class DriverInvitationsController : CallerController
{
    /// <summary>Bekleyen davetler.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DriverInvitationDto>>> List(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new MyInvitationsQuery { UserId = UserId, VerifiedPhone = VerifiedPhone }, cancellationToken));

    /// <summary>Daveti kabul eder; şoför firmanın şoförleri arasına girer.</summary>
    [HttpPost("{id:guid}/accept")]
    public Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken) => Respond(id, true, cancellationToken);

    /// <summary>Daveti reddeder.</summary>
    [HttpPost("{id:guid}/decline")]
    public Task<IActionResult> Decline(Guid id, CancellationToken cancellationToken) => Respond(id, false, cancellationToken);

    private async Task<IActionResult> Respond(Guid id, bool accept, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RespondInvitationCommand { Id = id, Accept = accept, UserId = UserId, VerifiedPhone = VerifiedPhone }, cancellationToken);
        return NoContent();
    }
}
