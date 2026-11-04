using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.Stations;

/// <summary>Kimliğe göre tek bir Station getirir.</summary>
public sealed class GetStationByIdQuery : IQuery<StationDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetStationByIdHandler : IQueryHandler<GetStationByIdQuery, StationDto?>
{
    private readonly IRepository<Station> _repository;

    public GetStationByIdHandler(IRepository<Station> repository) => _repository = repository;

    public async Task<StationDto?> Handle(GetStationByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : StationDto.From(entity);
    }
}

/// <summary>Station kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListStationQuery : PagedRequest, IQuery<PagedResult<StationDto>>
{
    /// <summary>Verilirse yalnızca IsActive değeri buna eşit kayıtlar döner (<c>?isactive=...</c>).</summary>
    public bool? IsActive { get; set; }
}

internal sealed class ListStationHandler : IQueryHandler<ListStationQuery, PagedResult<StationDto>>
{
    private readonly IRepository<Station> _repository;

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["Name", "Code", "Address", "Phone", "Latitude", "Longitude", "RadiusM", "IsActive", "CreatedAt", "UpdatedAt"];

    public ListStationHandler(IRepository<Station> repository) => _repository = repository;

    public async Task<PagedResult<StationDto>> Handle(ListStationQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var searchPattern = LikePattern.ContainsOrNull(request.Search) ?? string.Empty;
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            SortExpression.Normalize(request.SortBy, SortFields),
            query =>
            {
                if (request.IsActive is { } filterIsActive)
                {
                    query = query.Where(x => x.IsActive == filterIsActive);
                }

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike(x.Name, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike(x.Code, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.Address ?? string.Empty), searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.Phone ?? string.Empty), searchPattern, LikePattern.EscapeCharacter));
            },
            cancellationToken);

        return new PagedResult<StationDto>
        {
            Items = items.Select(StationDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
