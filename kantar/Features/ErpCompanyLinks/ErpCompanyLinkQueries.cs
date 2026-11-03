using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.ErpCompanyLinks;

/// <summary>Kimliğe göre tek bir ErpCompanyLink getirir.</summary>
public sealed class GetErpCompanyLinkByIdQuery : IQuery<ErpCompanyLinkDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetErpCompanyLinkByIdHandler : IQueryHandler<GetErpCompanyLinkByIdQuery, ErpCompanyLinkDto?>
{
    private readonly IRepository<ErpCompanyLink> _repository;

    public GetErpCompanyLinkByIdHandler(IRepository<ErpCompanyLink> repository) => _repository = repository;

    public async Task<ErpCompanyLinkDto?> Handle(GetErpCompanyLinkByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : ErpCompanyLinkDto.From(entity);
    }
}

/// <summary>ErpCompanyLink kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListErpCompanyLinkQuery : PagedRequest, IQuery<PagedResult<ErpCompanyLinkDto>>
{
    /// <summary>Verilirse yalnızca CompanyId değeri buna eşit kayıtlar döner (<c>?companyid=...</c>).</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Verilirse yalnızca ErpSystem değeri buna eşit kayıtlar döner (<c>?erpsystem=...</c>).</summary>
    public string? ErpSystem { get; set; }

    /// <summary>Verilirse yalnızca ErpCode değeri buna eşit kayıtlar döner (<c>?erpcode=...</c>).</summary>
    public string? ErpCode { get; set; }
}

internal sealed class ListErpCompanyLinkHandler : IQueryHandler<ListErpCompanyLinkQuery, PagedResult<ErpCompanyLinkDto>>
{
    private readonly IRepository<ErpCompanyLink> _repository;

    public ListErpCompanyLinkHandler(IRepository<ErpCompanyLink> repository) => _repository = repository;

    public async Task<PagedResult<ErpCompanyLinkDto>> Handle(ListErpCompanyLinkQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            request.SortBy,
            query =>
            {
                if (request.CompanyId is { } filterCompanyId)
                {
                    query = query.Where(x => x.CompanyId == filterCompanyId);
                }

                if (request.ErpSystem is { } filterErpSystem)
                {
                    query = query.Where(x => x.ErpSystem == filterErpSystem);
                }

                if (request.ErpCode is { } filterErpCode)
                {
                    query = query.Where(x => x.ErpCode == filterErpCode);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike(x.ErpSystem, $"%{request.Search}%") || EF.Functions.ILike(x.ErpCode, $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<ErpCompanyLinkDto>
        {
            Items = items.Select(ErpCompanyLinkDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
