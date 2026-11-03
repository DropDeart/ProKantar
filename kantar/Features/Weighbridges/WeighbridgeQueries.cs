using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.Weighbridges;

/// <summary>Kimliğe göre tek bir Weighbridge getirir.</summary>
public sealed class GetWeighbridgeByIdQuery : IQuery<WeighbridgeDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetWeighbridgeByIdHandler : IQueryHandler<GetWeighbridgeByIdQuery, WeighbridgeDto?>
{
    private readonly IRepository<Weighbridge> _repository;

    public GetWeighbridgeByIdHandler(IRepository<Weighbridge> repository) => _repository = repository;

    public async Task<WeighbridgeDto?> Handle(GetWeighbridgeByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : WeighbridgeDto.From(entity);
    }
}

/// <summary>Weighbridge kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListWeighbridgeQuery : PagedRequest, IQuery<PagedResult<WeighbridgeDto>>
{
    /// <summary>Verilirse yalnızca StationId değeri buna eşit kayıtlar döner (<c>?stationid=...</c>).</summary>
    public Guid? StationId { get; set; }

    /// <summary>Verilirse yalnızca IsActive değeri buna eşit kayıtlar döner (<c>?isactive=...</c>).</summary>
    public bool? IsActive { get; set; }
}

internal sealed class ListWeighbridgeHandler : IQueryHandler<ListWeighbridgeQuery, PagedResult<WeighbridgeDto>>
{
    private readonly IRepository<Weighbridge> _repository;

    public ListWeighbridgeHandler(IRepository<Weighbridge> repository) => _repository = repository;

    public async Task<PagedResult<WeighbridgeDto>> Handle(ListWeighbridgeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            request.SortBy,
            query =>
            {
                if (request.StationId is { } filterStationId)
                {
                    query = query.Where(x => x.StationId == filterStationId);
                }

                if (request.IsActive is { } filterIsActive)
                {
                    query = query.Where(x => x.IsActive == filterIsActive);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike(x.Name, $"%{request.Search}%") || EF.Functions.ILike((x.SerialNo ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike((x.ConnectionInfo ?? string.Empty), $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<WeighbridgeDto>
        {
            Items = items.Select(WeighbridgeDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
