using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kantar.Features.VehicleTypes;

namespace Kantar.Controllers;

/// <summary>VehicleType CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class VehicleTypesController : BaseController
{
    /// <summary>Kimliğe göre tek bir VehicleType getirir.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VehicleTypeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetVehicleTypeByIdQuery { Id = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>VehicleType kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search).</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<VehicleTypeDto>>> List([FromQuery] ListVehicleTypeQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni bir VehicleType oluşturur.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateVehicleTypeCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Var olan bir VehicleType kaydını günceller.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateVehicleTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Bir VehicleType kaydını siler.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteVehicleTypeCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
