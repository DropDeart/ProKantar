using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Features.Devices;

namespace Platform.Controllers;

/// <summary>Device CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class DevicesController : BaseController
{
    /// <summary>Kimliğe göre tek bir Device getirir. Sahibi olmayanlara başkasının kaydı 404 döner.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeviceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetDeviceByIdQuery
        {
            Id = id,
            RestrictToOwnerId = OwnerRestriction("SuperAdmin"),
        };
        var result = await Mediator.Send(query, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Device kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search). Sahibi olmayanlar yalnızca kendi kayıtlarını görür.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<DeviceDto>>> List([FromQuery] ListDeviceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        query.RestrictToOwnerId = OwnerRestriction("SuperAdmin");
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Yeni bir Device oluşturur. UserId istekten okunmaz, çağıranın kimliğiyle doldurulur.</summary>
    [Authorize(Roles = "Driver,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateDeviceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.UserId = CurrentUserId ?? throw new ForbiddenException("Kullanıcı kimliği (sub) token'da bulunamadı.");
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Var olan bir Device kaydını günceller. Sahibi olmayanlara 403 döner.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDeviceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        command.RestrictToOwnerId = OwnerRestriction("SuperAdmin");
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Bir Device kaydını siler. Sahibi olmayanlara 403 döner.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteDeviceCommand { Id = id, RestrictToOwnerId = OwnerRestriction("SuperAdmin") }, cancellationToken);
        return NoContent();
    }
}
