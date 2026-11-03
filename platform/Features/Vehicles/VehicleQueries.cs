using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Platform.Entities;
using Microsoft.EntityFrameworkCore;

namespace Platform.Features.Vehicles;

/// <summary>Kimliğe göre tek bir Vehicle getirir.</summary>
public sealed class GetVehicleByIdQuery : IQuery<VehicleDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıt döner (başkasınınki bulunamadı sayılır).
    /// Controller doldurur; gRPC gibi servisler arası çağrılarda boş kalır (bkz. docs/ARCH.md §6.1).
    /// </summary>
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class GetVehicleByIdHandler : IQueryHandler<GetVehicleByIdQuery, VehicleDto?>
{
    private readonly IRepository<Vehicle> _repository;

    public GetVehicleByIdHandler(IRepository<Vehicle> repository) => _repository = repository;

    public async Task<VehicleDto?> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (request.RestrictToOwnerId is { } ownerId && entity.OwnerUserId != ownerId)
        {
            return null;
        }

        return VehicleDto.From(entity);
    }
}

/// <summary>Vehicle kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListVehicleQuery : PagedRequest, IQuery<PagedResult<VehicleDto>>
{
    /// <summary>Verilirse yalnızca Plate değeri buna eşit kayıtlar döner (<c>?plate=...</c>).</summary>
    public string? Plate { get; set; }

    /// <summary>Verilirse yalnızca CarrierId değeri buna eşit kayıtlar döner (<c>?carrierid=...</c>).</summary>
    public Guid? CarrierId { get; set; }

    /// <summary>Verilirse yalnızca IsClaimed değeri buna eşit kayıtlar döner (<c>?isclaimed=...</c>).</summary>
    public bool? IsClaimed { get; set; }

    /// <summary>Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıtlar döner. İstemciden bağlanmaz; controller doldurur.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class ListVehicleHandler : IQueryHandler<ListVehicleQuery, PagedResult<VehicleDto>>
{
    private readonly IRepository<Vehicle> _repository;

    public ListVehicleHandler(IRepository<Vehicle> repository) => _repository = repository;

    public async Task<PagedResult<VehicleDto>> Handle(ListVehicleQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            request.SortBy,
            query =>
            {
                if (request.RestrictToOwnerId is { } ownerId)
                {
                    query = query.Where(x => x.OwnerUserId == ownerId);
                }

                if (request.Plate is { } filterPlate)
                {
                    query = query.Where(x => x.Plate == filterPlate);
                }

                if (request.CarrierId is { } filterCarrierId)
                {
                    query = query.Where(x => x.CarrierId == filterCarrierId);
                }

                if (request.IsClaimed is { } filterIsClaimed)
                {
                    query = query.Where(x => x.IsClaimed == filterIsClaimed);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike(x.Plate, $"%{request.Search}%") || EF.Functions.ILike((x.VehicleTypeCode ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike((x.Brand ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike((x.Model ?? string.Empty), $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<VehicleDto>
        {
            Items = items.Select(VehicleDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
