using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Features.Carriers;

namespace Platform.Controllers;

/// <summary>Carrier CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class CarriersController : BaseController
{
    /// <summary>Kimliğe göre tek bir Carrier getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CarrierDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCarrierByIdQuery { Id = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Carrier kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CarrierDto>>> List([FromQuery] ListCarrierQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni bir Carrier oluşturur. OwnerUserId istekten okunmaz, çağıranın kimliğiyle doldurulur.</summary>
    [Authorize(Roles = "Driver,TenantAdmin,Operator,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateCarrierCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.OwnerUserId = CurrentUserId ?? throw new ForbiddenException("Kullanıcı kimliği (sub) token'da bulunamadı.");
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Var olan bir Carrier kaydını günceller. Sahibi olmayanlara 403 döner.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCarrierCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        command.RestrictToOwnerId = OwnerRestriction("SuperAdmin");
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Bir Carrier kaydını siler.</summary>
    [Authorize(Roles = "SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteCarrierCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
