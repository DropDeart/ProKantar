using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.Shipments;

/// <summary>Kimliğe göre tek bir Shipment getirir.</summary>
public sealed class GetShipmentByIdQuery : IQuery<ShipmentDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetShipmentByIdHandler : IQueryHandler<GetShipmentByIdQuery, ShipmentDto?>
{
    private readonly IRepository<Shipment> _repository;

    public GetShipmentByIdHandler(IRepository<Shipment> repository) => _repository = repository;

    public async Task<ShipmentDto?> Handle(GetShipmentByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : ShipmentDto.From(entity);
    }
}

/// <summary>Shipment kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListShipmentQuery : PagedRequest, IQuery<PagedResult<ShipmentDto>>
{
    /// <summary>Verilirse yalnızca StationId değeri buna eşit kayıtlar döner (<c>?stationid=...</c>).</summary>
    public Guid? StationId { get; set; }

    /// <summary>Verilirse yalnızca VehicleId değeri buna eşit kayıtlar döner (<c>?vehicleid=...</c>).</summary>
    public Guid? VehicleId { get; set; }

    /// <summary>Verilirse yalnızca DriverId değeri buna eşit kayıtlar döner (<c>?driverid=...</c>).</summary>
    public Guid? DriverId { get; set; }

    /// <summary>Verilirse yalnızca Direction değeri buna eşit kayıtlar döner (<c>?direction=...</c>).</summary>
    public ShipmentDirection? Direction { get; set; }

    /// <summary>Verilirse yalnızca Status değeri buna eşit kayıtlar döner (<c>?status=...</c>).</summary>
    public ShipmentStatus? Status { get; set; }

    /// <summary>Verilirse yalnızca Plate değeri buna eşit kayıtlar döner (<c>?plate=...</c>).</summary>
    public string? Plate { get; set; }
}

internal sealed class ListShipmentHandler : IQueryHandler<ListShipmentQuery, PagedResult<ShipmentDto>>
{
    private readonly IRepository<Shipment> _repository;

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["ShipmentNo", "Direction", "Status", "PlannedAt", "Plate", "DriverId", "DriverName", "DispatchedAt", "Note", "StationId", "VehicleId", "CreatedAt", "UpdatedAt"];

    public ListShipmentHandler(IRepository<Shipment> repository) => _repository = repository;

    public async Task<PagedResult<ShipmentDto>> Handle(ListShipmentQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var searchPattern = LikePattern.ContainsOrNull(request.Search) ?? string.Empty;
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            SortExpression.Normalize(request.SortBy, SortFields),
            query =>
            {
                if (request.StationId is { } filterStationId)
                {
                    query = query.Where(x => x.StationId == filterStationId);
                }

                if (request.VehicleId is { } filterVehicleId)
                {
                    query = query.Where(x => x.VehicleId == filterVehicleId);
                }

                if (request.DriverId is { } filterDriverId)
                {
                    query = query.Where(x => x.DriverId == filterDriverId);
                }

                if (request.Direction is { } filterDirection)
                {
                    query = query.Where(x => x.Direction == filterDirection);
                }

                if (request.Status is { } filterStatus)
                {
                    query = query.Where(x => x.Status == filterStatus);
                }

                if (request.Plate is { } filterPlate)
                {
                    query = query.Where(x => x.Plate == filterPlate);
                }

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike(x.ShipmentNo, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike(x.Plate, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.DriverName ?? string.Empty), searchPattern, LikePattern.EscapeCharacter));
            },
            cancellationToken);

        return new PagedResult<ShipmentDto>
        {
            Items = items.Select(ShipmentDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
