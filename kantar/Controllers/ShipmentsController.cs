using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kantar.Features.Shipments;

namespace Kantar.Controllers;

/// <summary>Shipment CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class ShipmentsController : BaseController
{
    /// <summary>Kimliğe göre tek bir Shipment getirir.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShipmentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetShipmentByIdQuery { Id = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Shipment kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search).</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ShipmentDto>>> List([FromQuery] ListShipmentQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni bir Shipment oluşturur.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateShipmentCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Var olan bir Shipment kaydını günceller.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateShipmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Bir Shipment kaydını siler.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteShipmentCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
