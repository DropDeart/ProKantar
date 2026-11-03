using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kantar.Features.WeighingPhotos;

namespace Kantar.Controllers;

/// <summary>WeighingPhoto CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class WeighingPhotosController : BaseController
{
    /// <summary>Kimliğe göre tek bir WeighingPhoto getirir.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WeighingPhotoDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetWeighingPhotoByIdQuery { Id = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>WeighingPhoto kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search).</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<WeighingPhotoDto>>> List([FromQuery] ListWeighingPhotoQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni bir WeighingPhoto oluşturur.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateWeighingPhotoCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }


}
