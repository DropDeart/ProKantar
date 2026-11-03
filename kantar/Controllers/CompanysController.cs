using BaseForge.API.Controllers;
using BaseForge.Core.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kantar.Features.Companys;

namespace Kantar.Controllers;

/// <summary>Company CRUD uçları.</summary>
[Authorize]
[Route("api/[controller]")]
public sealed class CompanysController : BaseController
{
    /// <summary>Kimliğe göre tek bir Company getirir.</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCompanyByIdQuery { Id = id }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Company kayıtlarını sayfalı listeler (query string: page, pageSize, sortBy, search).</summary>
    [Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<CompanyDto>>> List([FromQuery] ListCompanyQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni bir Company oluşturur.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        var id = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Var olan bir Company kaydını günceller.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCompanyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Bir Company kaydını siler.</summary>
    [Authorize(Roles = "TenantAdmin,SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteCompanyCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
