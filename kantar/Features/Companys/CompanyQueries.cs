using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.Companys;

/// <summary>Kimliğe göre tek bir Company getirir.</summary>
public sealed class GetCompanyByIdQuery : IQuery<CompanyDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetCompanyByIdHandler : IQueryHandler<GetCompanyByIdQuery, CompanyDto?>
{
    private readonly IRepository<Company> _repository;

    public GetCompanyByIdHandler(IRepository<Company> repository) => _repository = repository;

    public async Task<CompanyDto?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : CompanyDto.From(entity);
    }
}

/// <summary>Company kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListCompanyQuery : PagedRequest, IQuery<PagedResult<CompanyDto>>
{
    /// <summary>Verilirse yalnızca IsCustomer değeri buna eşit kayıtlar döner (<c>?iscustomer=...</c>).</summary>
    public bool? IsCustomer { get; set; }

    /// <summary>Verilirse yalnızca IsSupplier değeri buna eşit kayıtlar döner (<c>?issupplier=...</c>).</summary>
    public bool? IsSupplier { get; set; }

    /// <summary>Verilirse yalnızca IsCarrier değeri buna eşit kayıtlar döner (<c>?iscarrier=...</c>).</summary>
    public bool? IsCarrier { get; set; }

    /// <summary>Verilirse yalnızca IsActive değeri buna eşit kayıtlar döner (<c>?isactive=...</c>).</summary>
    public bool? IsActive { get; set; }

    /// <summary>Verilirse yalnızca TaxNo değeri buna eşit kayıtlar döner (<c>?taxno=...</c>).</summary>
    public string? TaxNo { get; set; }
}

internal sealed class ListCompanyHandler : IQueryHandler<ListCompanyQuery, PagedResult<CompanyDto>>
{
    private readonly IRepository<Company> _repository;

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["Name", "ShortName", "TaxNo", "TaxOffice", "IsCustomer", "IsSupplier", "IsCarrier", "Phone", "Email", "Address", "IsActive", "CreatedAt", "UpdatedAt"];

    public ListCompanyHandler(IRepository<Company> repository) => _repository = repository;

    public async Task<PagedResult<CompanyDto>> Handle(ListCompanyQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var searchPattern = LikePattern.ContainsOrNull(request.Search) ?? string.Empty;
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            SortExpression.Normalize(request.SortBy, SortFields),
            query =>
            {
                if (request.IsCustomer is { } filterIsCustomer)
                {
                    query = query.Where(x => x.IsCustomer == filterIsCustomer);
                }

                if (request.IsSupplier is { } filterIsSupplier)
                {
                    query = query.Where(x => x.IsSupplier == filterIsSupplier);
                }

                if (request.IsCarrier is { } filterIsCarrier)
                {
                    query = query.Where(x => x.IsCarrier == filterIsCarrier);
                }

                if (request.IsActive is { } filterIsActive)
                {
                    query = query.Where(x => x.IsActive == filterIsActive);
                }

                if (request.TaxNo is { } filterTaxNo)
                {
                    query = query.Where(x => x.TaxNo == filterTaxNo);
                }

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike(x.Name, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.ShortName ?? string.Empty), searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.TaxNo ?? string.Empty), searchPattern, LikePattern.EscapeCharacter));
            },
            cancellationToken);

        return new PagedResult<CompanyDto>
        {
            Items = items.Select(CompanyDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
