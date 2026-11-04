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

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["Code", "Name", "MaxGrossKg", "CreatedAt", "UpdatedAt"];

    public ListVehicleTypeHandler(IRepository<VehicleType> repository) => _repository = repository;

    public async Task<PagedResult<VehicleTypeDto>> Handle(ListVehicleTypeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var searchPattern = LikePattern.ContainsOrNull(request.Search) ?? string.Empty;
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            SortExpression.Normalize(request.SortBy, SortFields),
            query =>
            {
                if (request.Code is { } filterCode)
                {
                    query = query.Where(x => x.Code == filterCode);
                }

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike(x.Code, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike(x.Name, searchPattern, LikePattern.EscapeCharacter));
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
