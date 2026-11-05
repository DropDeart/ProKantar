using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Features.Drivers;

namespace Platform.Controllers;

/// <summary>Driver CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class DriversController : BaseController
{
    /// <summary>Kimliğe göre tek bir Driver getirir. Sahibi olmayanlara başkasının kaydı 404 döner.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DriverDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetDriverByIdQuery
        {
            Id = id,
            RestrictToOwnerId = OwnerRestriction("SuperAdmin"),
        };
        var result = await Mediator.Send(query, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Driver kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search). Sahibi olmayanlar yalnızca kendi kayıtlarını görür.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<DriverDto>>> List([FromQuery] ListDriverQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        query.RestrictToOwnerId = OwnerRestriction("SuperAdmin");
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Yeni bir Driver oluşturur. UserId istekten okunmaz, çağıranın kimliğiyle doldurulur.</summary>
    [Authorize(Roles = "Driver,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateDriverCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.UserId = CurrentUserId ?? throw new ForbiddenException("Kullanıcı kimliği (sub) token'da bulunamadı.");
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Var olan bir Driver kaydını günceller. Sahibi olmayanlara 403 döner.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDriverCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        command.RestrictToOwnerId = OwnerRestriction("SuperAdmin");
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Bir Driver kaydını siler.</summary>
    [Authorize(Roles = "SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteDriverCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
