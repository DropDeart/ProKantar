using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.Waybills;

/// <summary>Kimliğe göre tek bir Waybill getirir.</summary>
public sealed class GetWaybillByIdQuery : IQuery<WaybillDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetWaybillByIdHandler : IQueryHandler<GetWaybillByIdQuery, WaybillDto?>
{
    private readonly IRepository<Waybill> _repository;

    public GetWaybillByIdHandler(IRepository<Waybill> repository) => _repository = repository;

    public async Task<WaybillDto?> Handle(GetWaybillByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : WaybillDto.From(entity);
    }
}

/// <summary>Waybill kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListWaybillQuery : PagedRequest, IQuery<PagedResult<WaybillDto>>
{
    /// <summary>Verilirse yalnızca WeighingId değeri buna eşit kayıtlar döner (<c>?weighingid=...</c>).</summary>
    public Guid? WeighingId { get; set; }

    /// <summary>Verilirse yalnızca CompanyId değeri buna eşit kayıtlar döner (<c>?companyid=...</c>).</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Verilirse yalnızca DeliveryLocationId değeri buna eşit kayıtlar döner (<c>?deliverylocationid=...</c>).</summary>
    public Guid? DeliveryLocationId { get; set; }

    /// <summary>Verilirse yalnızca WaybillNo değeri buna eşit kayıtlar döner (<c>?waybillno=...</c>).</summary>
    public string? WaybillNo { get; set; }

    /// <summary>Verilirse yalnızca WaybillType değeri buna eşit kayıtlar döner (<c>?waybilltype=...</c>).</summary>
    public WaybillWaybillType? WaybillType { get; set; }

    /// <summary>Verilirse yalnızca ErpSyncStatus değeri buna eşit kayıtlar döner (<c>?erpsyncstatus=...</c>).</summary>
    public WaybillErpSyncStatus? ErpSyncStatus { get; set; }

    /// <summary>Verilirse yalnızca EDespatchStatus değeri buna eşit kayıtlar döner (<c>?edespatchstatus=...</c>).</summary>
    public WaybillEDespatchStatus? EDespatchStatus { get; set; }
}

internal sealed class ListWaybillHandler : IQueryHandler<ListWaybillQuery, PagedResult<WaybillDto>>
{
    private readonly IRepository<Waybill> _repository;

    public ListWaybillHandler(IRepository<Waybill> repository) => _repository = repository;

    public async Task<PagedResult<WaybillDto>> Handle(ListWaybillQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            request.SortBy,
            query =>
            {
                if (request.WeighingId is { } filterWeighingId)
                {
                    query = query.Where(x => x.WeighingId == filterWeighingId);
                }

                if (request.CompanyId is { } filterCompanyId)
                {
                    query = query.Where(x => x.CompanyId == filterCompanyId);
                }

                if (request.DeliveryLocationId is { } filterDeliveryLocationId)
                {
                    query = query.Where(x => x.DeliveryLocationId == filterDeliveryLocationId);
                }

                if (request.WaybillNo is { } filterWaybillNo)
                {
                    query = query.Where(x => x.WaybillNo == filterWaybillNo);
                }

                if (request.WaybillType is { } filterWaybillType)
                {
                    query = query.Where(x => x.WaybillType == filterWaybillType);
                }

                if (request.ErpSyncStatus is { } filterErpSyncStatus)
                {
                    query = query.Where(x => x.ErpSyncStatus == filterErpSyncStatus);
                }

                if (request.EDespatchStatus is { } filterEDespatchStatus)
                {
                    query = query.Where(x => x.EDespatchStatus == filterEDespatchStatus);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike(x.WaybillNo, $"%{request.Search}%") || EF.Functions.ILike((x.DeliveryAddress ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike((x.ErpDocNo ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike((x.ErpError ?? string.Empty), $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<WaybillDto>
        {
            Items = items.Select(WaybillDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
