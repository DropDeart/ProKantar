using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kantar.Features.Stations;

namespace Kantar.Controllers;

/// <summary>Station CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class StationsController : BaseController
{
    /// <summary>Kimliğe göre tek bir Station getirir.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetStationByIdQuery { Id = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Station kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search).</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<StationDto>>> List([FromQuery] ListStationQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni bir Station oluşturur.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateStationCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Var olan bir Station kaydını günceller.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateStationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Bir Station kaydını siler.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteStationCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
