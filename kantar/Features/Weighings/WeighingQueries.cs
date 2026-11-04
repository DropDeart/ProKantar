using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.Weighings;

/// <summary>Kimliğe göre tek bir Weighing getirir.</summary>
public sealed class GetWeighingByIdQuery : IQuery<WeighingDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetWeighingByIdHandler : IQueryHandler<GetWeighingByIdQuery, WeighingDto?>
{
    private readonly IRepository<Weighing> _repository;

    public GetWeighingByIdHandler(IRepository<Weighing> repository) => _repository = repository;

    public async Task<WeighingDto?> Handle(GetWeighingByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : WeighingDto.From(entity);
    }
}

/// <summary>Weighing kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListWeighingQuery : PagedRequest, IQuery<PagedResult<WeighingDto>>
{
    /// <summary>Verilirse yalnızca StationId değeri buna eşit kayıtlar döner (<c>?stationid=...</c>).</summary>
    public Guid? StationId { get; set; }

    /// <summary>Verilirse yalnızca WeighbridgeId değeri buna eşit kayıtlar döner (<c>?weighbridgeid=...</c>).</summary>
    public Guid? WeighbridgeId { get; set; }

    /// <summary>Verilirse yalnızca CompanyId değeri buna eşit kayıtlar döner (<c>?companyid=...</c>).</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Verilirse yalnızca MaterialId değeri buna eşit kayıtlar döner (<c>?materialid=...</c>).</summary>
    public Guid? MaterialId { get; set; }

    /// <summary>Verilirse yalnızca VehicleId değeri buna eşit kayıtlar döner (<c>?vehicleid=...</c>).</summary>
    public Guid? VehicleId { get; set; }

    /// <summary>Verilirse yalnızca DriverId değeri buna eşit kayıtlar döner (<c>?driverid=...</c>).</summary>
    public Guid? DriverId { get; set; }

    /// <summary>Verilirse yalnızca Status değeri buna eşit kayıtlar döner (<c>?status=...</c>).</summary>
    public WeighingStatus? Status { get; set; }

    /// <summary>Verilirse yalnızca Direction değeri buna eşit kayıtlar döner (<c>?direction=...</c>).</summary>
    public WeighingDirection? Direction { get; set; }

    /// <summary>Verilirse yalnızca Plate değeri buna eşit kayıtlar döner (<c>?plate=...</c>).</summary>
    public string? Plate { get; set; }

    /// <summary>Verilirse yalnızca TicketNo değeri buna eşit kayıtlar döner (<c>?ticketno=...</c>).</summary>
    public string? TicketNo { get; set; }
}

internal sealed class ListWeighingHandler : IQueryHandler<ListWeighingQuery, PagedResult<WeighingDto>>
{
    private readonly IRepository<Weighing> _repository;

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["TicketNo", "Direction", "Status", "Plate", "TrailerId", "TrailerPlate", "DriverId", "DriverName", "DriverPhone", "FirstWeightKg", "FirstWeighedAt", "FirstIsManual", "SecondWeighbridgeId", "SecondWeightKg", "SecondWeighedAt", "SecondIsManual", "GrossKg", "TareKg", "NetKg", "TareSource", "CancelReason", "Note", "StationId", "WeighbridgeId", "CompanyId", "MaterialId", "ShipmentId", "VehicleId", "CreatedAt", "UpdatedAt"];

    public ListWeighingHandler(IRepository<Weighing> repository) => _repository = repository;

    public async Task<PagedResult<WeighingDto>> Handle(ListWeighingQuery request, CancellationToken cancellationToken)
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

                if (request.WeighbridgeId is { } filterWeighbridgeId)
                {
                    query = query.Where(x => x.WeighbridgeId == filterWeighbridgeId);
                }

                if (request.CompanyId is { } filterCompanyId)
                {
                    query = query.Where(x => x.CompanyId == filterCompanyId);
                }

                if (request.MaterialId is { } filterMaterialId)
                {
                    query = query.Where(x => x.MaterialId == filterMaterialId);
                }

                if (request.VehicleId is { } filterVehicleId)
                {
                    query = query.Where(x => x.VehicleId == filterVehicleId);
                }

                if (request.DriverId is { } filterDriverId)
                {
                    query = query.Where(x => x.DriverId == filterDriverId);
                }

                if (request.Status is { } filterStatus)
                {
                    query = query.Where(x => x.Status == filterStatus);
                }

                if (request.Direction is { } filterDirection)
                {
                    query = query.Where(x => x.Direction == filterDirection);
                }

                if (request.Plate is { } filterPlate)
                {
                    query = query.Where(x => x.Plate == filterPlate);
                }

                if (request.TicketNo is { } filterTicketNo)
                {
                    query = query.Where(x => x.TicketNo == filterTicketNo);
                }

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike(x.TicketNo, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike(x.Plate, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.TrailerPlate ?? string.Empty), searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.DriverName ?? string.Empty), searchPattern, LikePattern.EscapeCharacter));
            },
            cancellationToken);

        return new PagedResult<WeighingDto>
        {
            Items = items.Select(WeighingDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
