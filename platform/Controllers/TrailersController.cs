using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Features.Trailers;

namespace Platform.Controllers;

/// <summary>Trailer CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class TrailersController : BaseController
{
    /// <summary>Kimliğe göre tek bir Trailer getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TrailerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTrailerByIdQuery { Id = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Trailer kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TrailerDto>>> List([FromQuery] ListTrailerQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni bir Trailer oluşturur. OwnerUserId istekten okunmaz, çağıranın kimliğiyle doldurulur.</summary>
    [Authorize(Roles = "Driver,Operator,TenantAdmin,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTrailerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.OwnerUserId = CurrentUserId ?? throw new ForbiddenException("Kullanıcı kimliği (sub) token'da bulunamadı.");
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Var olan bir Trailer kaydını günceller. Sahibi olmayanlara 403 döner.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTrailerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        command.RestrictToOwnerId = OwnerRestriction("SuperAdmin");
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Bir Trailer kaydını siler.</summary>
    [Authorize(Roles = "SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteTrailerCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
