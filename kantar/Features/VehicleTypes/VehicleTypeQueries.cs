using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.VehicleTypes;

/// <summary>Kimliğe göre tek bir VehicleType getirir.</summary>
public sealed class GetVehicleTypeByIdQuery : IQuery<VehicleTypeDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetVehicleTypeByIdHandler : IQueryHandler<GetVehicleTypeByIdQuery, VehicleTypeDto?>
{
    private readonly IRepository<VehicleType> _repository;

    public GetVehicleTypeByIdHandler(IRepository<VehicleType> repository) => _repository = repository;

    public async Task<VehicleTypeDto?> Handle(GetVehicleTypeByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : VehicleTypeDto.From(entity);
    }
}

/// <summary>VehicleType kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListVehicleTypeQuery : PagedRequest, IQuery<PagedResult<VehicleTypeDto>>
{
    /// <summary>Verilirse yalnızca Code değeri buna eşit kayıtlar döner (<c>?code=...</c>).</summary>
    public string? Code { get; set; }
}

internal sealed class ListVehicleTypeHandler : IQueryHandler<ListVehicleTypeQuery, PagedResult<VehicleTypeDto>>
{
    private readonly IRepository<VehicleType> _repository;

    public ListVehicleTypeHandler(IRepository<VehicleType> repository) => _repository = repository;

    public async Task<PagedResult<VehicleTypeDto>> Handle(ListVehicleTypeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            request.SortBy,
            query =>
            {
                if (request.Code is { } filterCode)
                {
                    query = query.Where(x => x.Code == filterCode);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike(x.Code, $"%{request.Search}%") || EF.Functions.ILike(x.Name, $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<VehicleTypeDto>
        {
            Items = items.Select(VehicleTypeDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
