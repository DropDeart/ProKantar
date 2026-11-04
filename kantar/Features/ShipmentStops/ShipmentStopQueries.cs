using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.ShipmentStops;

/// <summary>Kimliğe göre tek bir ShipmentStop getirir.</summary>
public sealed class GetShipmentStopByIdQuery : IQuery<ShipmentStopDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetShipmentStopByIdHandler : IQueryHandler<GetShipmentStopByIdQuery, ShipmentStopDto?>
{
    private readonly IRepository<ShipmentStop> _repository;

    public GetShipmentStopByIdHandler(IRepository<ShipmentStop> repository) => _repository = repository;

    public async Task<ShipmentStopDto?> Handle(GetShipmentStopByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : ShipmentStopDto.From(entity);
    }
}

/// <summary>ShipmentStop kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListShipmentStopQuery : PagedRequest, IQuery<PagedResult<ShipmentStopDto>>
{
    /// <summary>Verilirse yalnızca ShipmentId değeri buna eşit kayıtlar döner (<c>?shipmentid=...</c>).</summary>
    public Guid? ShipmentId { get; set; }

    /// <summary>Verilirse yalnızca StopType değeri buna eşit kayıtlar döner (<c>?stoptype=...</c>).</summary>
    public ShipmentStopStopType? StopType { get; set; }

    /// <summary>Verilirse yalnızca WaybillId değeri buna eşit kayıtlar döner (<c>?waybillid=...</c>).</summary>
    public Guid? WaybillId { get; set; }
}

internal sealed class ListShipmentStopHandler : IQueryHandler<ListShipmentStopQuery, PagedResult<ShipmentStopDto>>
{
    private readonly IRepository<ShipmentStop> _repository;

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["Sequence", "StopType", "Name", "Latitude", "Longitude", "RadiusM", "PlannedAt", "Note", "ShipmentId", "DeliveryLocationId", "StationId", "WaybillId", "CreatedAt", "UpdatedAt"];

    public ListShipmentStopHandler(IRepository<ShipmentStop> repository) => _repository = repository;

    public async Task<PagedResult<ShipmentStopDto>> Handle(ListShipmentStopQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var searchPattern = LikePattern.ContainsOrNull(request.Search) ?? string.Empty;
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            SortExpression.Normalize(request.SortBy, SortFields),
            query =>
            {
                if (request.ShipmentId is { } filterShipmentId)
                {
                    query = query.Where(x => x.ShipmentId == filterShipmentId);
                }

                if (request.StopType is { } filterStopType)
                {
                    query = query.Where(x => x.StopType == filterStopType);
                }

                if (request.WaybillId is { } filterWaybillId)
                {
                    query = query.Where(x => x.WaybillId == filterWaybillId);
                }

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike(x.Name, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.Note ?? string.Empty), searchPattern, LikePattern.EscapeCharacter));
            },
            cancellationToken);

        return new PagedResult<ShipmentStopDto>
        {
            Items = items.Select(ShipmentStopDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
