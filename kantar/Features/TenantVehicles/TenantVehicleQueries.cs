using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.TenantVehicles;

/// <summary>Kimliğe göre tek bir TenantVehicle getirir.</summary>
public sealed class GetTenantVehicleByIdQuery : IQuery<TenantVehicleDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetTenantVehicleByIdHandler : IQueryHandler<GetTenantVehicleByIdQuery, TenantVehicleDto?>
{
    private readonly IRepository<TenantVehicle> _repository;

    public GetTenantVehicleByIdHandler(IRepository<TenantVehicle> repository) => _repository = repository;

    public async Task<TenantVehicleDto?> Handle(GetTenantVehicleByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : TenantVehicleDto.From(entity);
    }
}

/// <summary>TenantVehicle kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListTenantVehicleQuery : PagedRequest, IQuery<PagedResult<TenantVehicleDto>>
{
    /// <summary>Verilirse yalnızca VehicleId değeri buna eşit kayıtlar döner (<c>?vehicleid=...</c>).</summary>
    public Guid? VehicleId { get; set; }

    /// <summary>Verilirse yalnızca VehicleTypeId değeri buna eşit kayıtlar döner (<c>?vehicletypeid=...</c>).</summary>
    public Guid? VehicleTypeId { get; set; }

    /// <summary>Verilirse yalnızca IsBlacklisted değeri buna eşit kayıtlar döner (<c>?isblacklisted=...</c>).</summary>
    public bool? IsBlacklisted { get; set; }
}

internal sealed class ListTenantVehicleHandler : IQueryHandler<ListTenantVehicleQuery, PagedResult<TenantVehicleDto>>
{
    private readonly IRepository<TenantVehicle> _repository;

    public ListTenantVehicleHandler(IRepository<TenantVehicle> repository) => _repository = repository;

    public async Task<PagedResult<TenantVehicleDto>> Handle(ListTenantVehicleQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            request.SortBy,
            query =>
            {
                if (request.VehicleId is { } filterVehicleId)
                {
                    query = query.Where(x => x.VehicleId == filterVehicleId);
                }

                if (request.VehicleTypeId is { } filterVehicleTypeId)
                {
                    query = query.Where(x => x.VehicleTypeId == filterVehicleTypeId);
                }

                if (request.IsBlacklisted is { } filterIsBlacklisted)
                {
                    query = query.Where(x => x.IsBlacklisted == filterIsBlacklisted);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike((x.BlacklistReason ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike((x.Note ?? string.Empty), $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<TenantVehicleDto>
        {
            Items = items.Select(TenantVehicleDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
