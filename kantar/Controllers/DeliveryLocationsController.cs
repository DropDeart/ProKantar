using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kantar.Features.DeliveryLocations;

namespace Kantar.Controllers;

/// <summary>DeliveryLocation CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class DeliveryLocationsController : BaseController
{
    /// <summary>Kimliğe göre tek bir DeliveryLocation getirir.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeliveryLocationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetDeliveryLocationByIdQuery { Id = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>DeliveryLocation kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search).</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<DeliveryLocationDto>>> List([FromQuery] ListDeliveryLocationQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni bir DeliveryLocation oluşturur.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateDeliveryLocationCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Var olan bir DeliveryLocation kaydını günceller.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDeliveryLocationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Bir DeliveryLocation kaydını siler.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteDeliveryLocationCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
