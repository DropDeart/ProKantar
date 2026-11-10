using BaseForge.Core.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Custom.Fleet;

namespace Platform.Custom.Controllers;

/// <summary>Araç kataloğu (elle yazıldı; generate dokunmaz). Mobil ve panel araç seçiminde kullanılır.</summary>
[Authorize]
[Route("api/VehicleModels")]
public sealed class VehicleModelsController : CallerController
{
    /// <summary>Katalogdaki markalar.</summary>
    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<string>>> Brands(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new VehicleModelBrandsQuery(), cancellationToken));

    /// <summary>Seçilebilir modeller (marka, yapı, arama süzgeçli); yönetici <c>pending=true</c> ile önerileri de görür.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VehicleModelDto>>> List(
        [FromQuery] VehicleModelListQuery query, [FromQuery] bool pending, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        query.UserId = CurrentUserId;
        query.IncludePending = pending && IsInAnyRole("SuperAdmin");
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Katalogda olmayan modeli önerir; aynısı varsa onun kimliği döner.</summary>
    [Authorize(Roles = "Driver,Operator,TenantAdmin,SuperAdmin")]
    [HttpPost("suggestions")]
    public async Task<ActionResult<Guid>> Suggest(SuggestVehicleModelCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.UserId = UserId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>Yönetici: modeli düzeltir, onaylar ya da pasife alır.</summary>
    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateVehicleModelCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Id = id;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

/// <summary>Araç özelliği: katalog modeli + ruhsat değeri (elle yazıldı; generate dokunmaz).</summary>
[Authorize]
[Route("api/Vehicles")]
public sealed class VehicleSpecController : CallerController
{
    private static readonly string[] StaffRoles = ["TenantAdmin", "Operator"];

    /// <summary>Şoförün sahibi olduğu ya da aktif kullandığı araçlar (mobil).</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<VehicleSpecDto>>> Mine(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new MyVehiclesQuery { UserId = UserId }, cancellationToken));

    /// <summary>Aracın modeli, ruhsat değeri ve geçerli azami yüklü ağırlığı.</summary>
    [HttpGet("{id:guid}/spec")]
    public async Task<ActionResult<VehicleSpecDto>> GetSpec(Guid id, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new GetVehicleSpecQuery { VehicleId = id, TenantId = TenantIdOrNull }, cancellationToken));

    /// <summary>Aracın modelini ve ruhsat değerini yazar (sahibi, yetkili firma personeli ya da yönetici).</summary>
    [HttpPut("{id:guid}/spec")]
    public async Task<IActionResult> SetSpec(Guid id, SetVehicleSpecCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.VehicleId = id;
        command.Editor = new VehicleEditor(UserId, TenantIdOrNull, IsInAnyRole(StaffRoles), IsInAnyRole("SuperAdmin"));
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

/// <summary>Firmanın kendi araçları — "Araçlarımız" (elle yazıldı; generate dokunmaz).</summary>
[Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
[Route("api/Fleet")]
public sealed class FleetController : CallerController
{
    /// <summary>Firmanın araçları.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VehicleSpecDto>>> List(CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new FleetListQuery { TenantId = TenantId }, cancellationToken));

    /// <summary>Firmanın araçları, sayfalı: <c>?page=&amp;pageSize=&amp;search=</c> (liste ekranı; araç seçici tam listeyi kullanır).</summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<VehicleSpecDto>>> Paged([FromQuery] FleetPageQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        query.TenantId = TenantId;
        return Ok(await Mediator.Send(query, cancellationToken));
    }

    /// <summary>Filoya araç ekler (plaka platformda yoksa açılır); araç kimliğini döndürür.</summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> Add(AddFleetVehicleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.UserId = UserId;
        command.TenantId = TenantId;
        return Ok(await Mediator.Send(command, cancellationToken));
    }

    /// <summary>Filo aracına varsayılan şoför atar (<c>driverId: null</c> kaldırır); şoför onayı gerekmez.</summary>
    [HttpPut("{vehicleId:guid}/driver")]
    public async Task<IActionResult> SetDriver(Guid vehicleId, SetFleetDriverCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.VehicleId = vehicleId;
        command.TenantId = TenantId;
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Aracı filodan çıkarır.</summary>
    [HttpDelete("{vehicleId:guid}")]
    public async Task<IActionResult> Remove(Guid vehicleId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new RemoveFleetVehicleCommand { VehicleId = vehicleId, TenantId = TenantId }, cancellationToken);
        return NoContent();
    }
}
