using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Platform.Entities;

namespace Platform.Features.DriverVehicles;

/// <summary>Kimliğe göre tek bir DriverVehicle getirir.</summary>
public sealed class GetDriverVehicleByIdQuery : IQuery<DriverVehicleDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Doluysa yalnızca UserId'ı bu kullanıcı olan kayıt döner (başkasınınki bulunamadı sayılır).
    /// Controller doldurur; gRPC gibi servisler arası çağrılarda boş kalır (bkz. docs/ARCH.md §6.1).
    /// </summary>
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class GetDriverVehicleByIdHandler : IQueryHandler<GetDriverVehicleByIdQuery, DriverVehicleDto?>
{
    private readonly IRepository<DriverVehicle> _repository;

    public GetDriverVehicleByIdHandler(IRepository<DriverVehicle> repository) => _repository = repository;

    public async Task<DriverVehicleDto?> Handle(GetDriverVehicleByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (request.RestrictToOwnerId is { } ownerId && entity.UserId != ownerId)
        {
            return null;
        }

        return DriverVehicleDto.From(entity);
    }
}

/// <summary>DriverVehicle kayıtlarını sayfalı, sıralı biçimde listeler.</summary>
public sealed class ListDriverVehicleQuery : PagedRequest, IQuery<PagedResult<DriverVehicleDto>>
{
    /// <summary>Verilirse yalnızca DriverId değeri buna eşit kayıtlar döner (<c>?driverid=...</c>).</summary>
    public Guid? DriverId { get; set; }

    /// <summary>Verilirse yalnızca VehicleId değeri buna eşit kayıtlar döner (<c>?vehicleid=...</c>).</summary>
    public Guid? VehicleId { get; set; }

    /// <summary>Doluysa yalnızca UserId'ı bu kullanıcı olan kayıtlar döner. İstemciden bağlanmaz; controller doldurur.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class ListDriverVehicleHandler : IQueryHandler<ListDriverVehicleQuery, PagedResult<DriverVehicleDto>>
{
    private readonly IRepository<DriverVehicle> _repository;

    public ListDriverVehicleHandler(IRepository<DriverVehicle> repository) => _repository = repository;

    public async Task<PagedResult<DriverVehicleDto>> Handle(ListDriverVehicleQuery request, CancellationToken cancellationToken)
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
                    query = query.Where(x => x.UserId == ownerId);
                }

                if (request.DriverId is { } filterDriverId)
                {
                    query = query.Where(x => x.DriverId == filterDriverId);
                }

                if (request.VehicleId is { } filterVehicleId)
                {
                    query = query.Where(x => x.VehicleId == filterVehicleId);
                }

                return query;
            },
            cancellationToken);

        return new PagedResult<DriverVehicleDto>
        {
            Items = items.Select(DriverVehicleDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
