using BaseForge.Core.CQRS;
using BaseForge.API.Controllers;
using Kantar.Custom.Dashboard;
using Kantar.Custom.Directory;
using Kantar.Custom.EDespatch.Issuing;
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

    /// <summary>Son sevkiyatlarda kullanılan teslimat noktaları (hızlı seçim).</summary>
    [HttpGet("DeliveryLocations/recent")]
    public async Task<ActionResult<IReadOnlyList<RecentDeliveryLocationDto>>> RecentDeliveryLocations(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new RecentDeliveryLocationsQuery(), cancellationToken));

    /// <summary>Bağlı cari firmanın (ProKantar şirketi) teslimat noktası olarak seçilebilecek istasyonları.</summary>
    [HttpGet("Companys/{id:guid}/partner-stations")]
    public async Task<ActionResult<IReadOnlyList<PartnerStationDto>>> PartnerStations(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new PartnerStationsQuery { CompanyId = id }, cancellationToken));

    /// <summary>Sevkiyat listesi (güzergah özetiyle).</summary>
    [HttpGet("Shipments/board")]
    public async Task<ActionResult<PagedResult<ShipmentBoardItemDto>>> ShipmentBoard([FromQuery] ShipmentBoardQuery query, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(query, cancellationToken));

    /// <summary>Sevkiyat duraklarının yük satırları (planlanan miktar).</summary>
    [HttpGet("Shipments/{id:guid}/loads")]
    public async Task<ActionResult<IReadOnlyList<ShipmentStopLineDto>>> ShipmentLoads(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new ShipmentLoadsQuery { ShipmentId = id }, cancellationToken));

    /// <summary>Planlanan ile tartılan yan yana: durak başına plan ve irsaliye, tartımlar ve sapma.</summary>
    [HttpGet("Shipments/{id:guid}/progress")]
    public async Task<ActionResult<ShipmentProgressDto>> ShipmentProgress(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new ShipmentProgressQuery { ShipmentId = id }, cancellationToken));

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

    /// <summary>Taslak ya da yayınlanmış sevkiyatı gerekçeyle iptal eder; yayındaysa sefer kapanır.</summary>
    [HttpPost("Shipments/{id:guid}/cancel")]
    public async Task<IActionResult> CancelShipment(Guid id, CancelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await Mediator.Send(new CancelShipmentCommand { Id = id, Reason = request.Reason }, cancellationToken);
        return NoContent();
    }

    /// <summary>Açık tartımı gerekçeyle iptal eder.</summary>
    [HttpPost("Weighings/{id:guid}/cancel")]
    public async Task<IActionResult> CancelWeighing(Guid id, CancelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await Mediator.Send(new CancelWeighingCommand { Id = id, Reason = request.Reason }, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Açık tartımı tamamlar (brüt/dara/net sunucuda hesaplanır). Sevkiyata bağlı çıkış tartımında taslak irsaliyeler
    /// tartılan netle kesinleşir; eksik bilgili durak taslak kalır (tartım yine tamamlanır).
    /// </summary>
    [HttpPost("Weighings/{id:guid}/complete")]
    public async Task<IActionResult> CompleteWeighing(
        Guid id, CompleteWeighingCommand command, [FromServices] ShipmentWaybillIssuer issuer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(issuer);
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        await issuer.IssueForWeighingAsync(id, CancellationToken.None);
        return NoContent();
    }

    /// <summary>Yol ön izlemesi: durak sırasıyla yol çizgisi, durak arası mesafe ve süre (taslakta da).</summary>
    [HttpGet("Shipments/{id:guid}/route")]
    public async Task<ActionResult<Kantar.Custom.Shipments.Routing.ShipmentRoutePreview>> ShipmentRoute(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new Kantar.Custom.Shipments.Routing.ShipmentRouteQuery { ShipmentId = id }, cancellationToken));

    /// <summary>Sevkiyatın irsaliyeleri (durak başına): kesilmişse irsaliye, değilse plandan taslak ve eksikleri.</summary>
    [HttpGet("Shipments/{id:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<ShipmentDocument>>> ShipmentDocuments(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new ShipmentDocumentsQuery { ShipmentId = id }, cancellationToken));
}

/// <summary>İptal isteği gövdesi.</summary>
public sealed record CancelRequest(string? Reason);
