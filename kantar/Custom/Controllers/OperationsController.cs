using BaseForge.Core.CQRS;
using BaseForge.API.Controllers;
using Kantar.Custom.Dashboard;
using Kantar.Custom.Shipments;
using Kantar.Custom.Weighings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kantar.Custom.Controllers;

/// <summary>
/// CRUD dışı iş akışı uçları (elle yazıldı; generate dokunmaz). Route'lar generate edilen
/// <c>api/Shipments</c> ve <c>api/Weighings</c> kaynaklarının alt eylemleridir.
/// </summary>
[Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
[Route("api")]
public sealed class OperationsController : BaseController
{
    /// <summary>Panel ana sayfası özeti (bugün, sahadaki araçlar, son 7 gün).</summary>
    [HttpGet("Dashboard/summary")]
    public async Task<ActionResult<DashboardSummaryDto>> DashboardSummary(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new DashboardSummaryQuery(), cancellationToken));

    /// <summary>Sevkiyat listesi (güzergah özetiyle).</summary>
    [HttpGet("Shipments/board")]
    public async Task<ActionResult<PagedResult<ShipmentBoardItemDto>>> ShipmentBoard([FromQuery] ShipmentBoardQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Yeni sevkiyat planı (taslak) ve durakları.</summary>
    [HttpPost("Shipments/plan")]
    public async Task<ActionResult<Guid>> CreateShipmentPlan(SaveShipmentPlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = null;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>Taslak sevkiyatın planını (başlık + duraklar) değiştirir.</summary>
    [HttpPut("Shipments/{id:guid}/plan")]
    public async Task<IActionResult> UpdateShipmentPlan(Guid id, SaveShipmentPlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Sevkiyat planını yayınlar; tracking seferi açar.</summary>
    [HttpPost("Shipments/{id:guid}/dispatch")]
    public async Task<IActionResult> DispatchShipment(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DispatchShipmentCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>Açık tartımı tamamlar (brüt/dara/net sunucuda hesaplanır).</summary>
    [HttpPost("Weighings/{id:guid}/complete")]
    public async Task<IActionResult> CompleteWeighing(Guid id, CompleteWeighingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
