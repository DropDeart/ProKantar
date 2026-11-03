using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kantar.Features.WeighingAuditLogs;

namespace Kantar.Controllers;

/// <summary>WeighingAuditLog CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class WeighingAuditLogsController : BaseController
{
    /// <summary>Kimliğe göre tek bir WeighingAuditLog getirir.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WeighingAuditLogDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetWeighingAuditLogByIdQuery { Id = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>WeighingAuditLog kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search).</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<WeighingAuditLogDto>>> List([FromQuery] ListWeighingAuditLogQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni bir WeighingAuditLog oluşturur.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateWeighingAuditLogCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }


}
