using BaseForge.API.Controllers;
using Kantar.Custom.EDespatch.Issuing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kantar.Custom.EDespatch;

/// <summary>
/// Tartımdan irsaliye kesme ve irsaliye durum uçları (elle). Route'lar generate edilen <c>api/Weighings</c> ve
/// <c>api/Waybills</c> kaynaklarının alt eylemleridir.
/// </summary>
[Authorize(Roles = "TenantAdmin,Operator")]
[Route("api")]
public sealed class DespatchController : BaseController
{
    /// <summary>Tartımın irsaliyesi; kesilmemişse 404.</summary>
    [HttpGet("Weighings/{id:guid}/despatch")]
    public async Task<ActionResult<WaybillView>> GetForWeighing(Guid id, CancellationToken cancellationToken)
    {
        var view = await Mediator.Send(new GetWeighingDespatchQuery { WeighingId = id }, cancellationToken);
        return view is null ? NotFound() : Ok(view);
    }

    /// <summary>Kesmeden önce ön kontrol (numara ayırmaz, kayıt yazmaz).</summary>
    [HttpGet("Weighings/{id:guid}/despatch/preview")]
    public async Task<ActionResult<DespatchPreview>> Preview(Guid id, [FromQuery] Guid? deliveryLocationId, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new PreviewDespatchQuery { WeighingId = id, DeliveryLocationId = deliveryLocationId }, cancellationToken));

    /// <summary>Tamamlanmış çıkış tartımına irsaliye keser.</summary>
    [HttpPost("Weighings/{id:guid}/despatch")]
    public async Task<ActionResult<WaybillView>> Issue(Guid id, IssueDespatchRequest? request, CancellationToken cancellationToken)
    {
        var view = await Mediator.Send(new IssueDespatchCommand
        {
            WeighingId = id,
            DriverNationalId = request?.DriverNationalId,
            DeliveryLocationId = request?.DeliveryLocationId,
            Note = request?.Note,
        }, cancellationToken);
        return CreatedAtAction(nameof(GetForWeighing), new { id }, view);
    }

    /// <summary>UBL XML dosyası ({WaybillNo}.xml).</summary>
    [HttpGet("Waybills/{id:guid}/ubl")]
    public async Task<IActionResult> DownloadUbl(Guid id, CancellationToken cancellationToken)
    {
        var file = await Mediator.Send(new GetWaybillUblQuery { WaybillId = id }, cancellationToken);
        return File(file.Content, "application/xml", file.FileName);
    }

    /// <summary>Sağlayıcıdan güncel durumu çeker.</summary>
    [HttpPost("Waybills/{id:guid}/refresh-status")]
    public async Task<ActionResult<WaybillView>> RefreshStatus(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new RefreshWaybillStatusCommand { WaybillId = id }, cancellationToken));

    /// <summary>Elle yüklenen irsaliyenin durumunu işaretler.</summary>
    [HttpPost("Waybills/{id:guid}/mark")]
    public async Task<ActionResult<WaybillView>> Mark(Guid id, MarkWaybillRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Ok(await Mediator.Send(new MarkWaybillCommand { WaybillId = id, Status = request.Status, Note = request.Note }, cancellationToken));
    }

    /// <summary>Başarısız e-Logo gönderimini yeniden dener.</summary>
    [HttpPost("Waybills/{id:guid}/resend")]
    public async Task<ActionResult<WaybillView>> Resend(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new ResendWaybillCommand { WaybillId = id }, cancellationToken));
}

/// <summary>İrsaliye kesme gövdesi.</summary>
/// <param name="DriverNationalId">Platformda yoksa şoför TCKN'si.</param>
/// <param name="DeliveryLocationId">Sevkiyatsız tartımda teslim noktası.</param>
/// <param name="Note">Not.</param>
public sealed record IssueDespatchRequest(string? DriverNationalId, Guid? DeliveryLocationId, string? Note);

/// <summary>Elle durum işaretleme gövdesi.</summary>
/// <param name="Status">Sent, Delivered ya da Rejected.</param>
/// <param name="Note">Açıklama.</param>
public sealed record MarkWaybillRequest(ManualDespatchMark Status, string? Note);
