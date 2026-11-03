using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kantar.Features.ShipmentStops;

namespace Kantar.Controllers;

/// <summary>ShipmentStop CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class ShipmentStopsController : BaseController
{
    /// <summary>Kimliğe göre tek bir ShipmentStop getirir.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShipmentStopDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetShipmentStopByIdQuery { Id = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>ShipmentStop kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search).</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ShipmentStopDto>>> List([FromQuery] ListShipmentStopQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni bir ShipmentStop oluşturur.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateShipmentStopCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Var olan bir ShipmentStop kaydını günceller.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateShipmentStopCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Bir ShipmentStop kaydını siler.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteShipmentStopCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
