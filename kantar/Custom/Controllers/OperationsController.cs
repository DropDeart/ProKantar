using BaseForge.API.Controllers;
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
