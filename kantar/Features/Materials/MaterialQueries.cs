using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.Materials;

/// <summary>Kimliğe göre tek bir Material getirir.</summary>
public sealed class GetMaterialByIdQuery : IQuery<MaterialDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetMaterialByIdHandler : IQueryHandler<GetMaterialByIdQuery, MaterialDto?>
{
    private readonly IRepository<Material> _repository;

    public GetMaterialByIdHandler(IRepository<Material> repository) => _repository = repository;

    public async Task<MaterialDto?> Handle(GetMaterialByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : MaterialDto.From(entity);
    }
}

/// <summary>Material kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListMaterialQuery : PagedRequest, IQuery<PagedResult<MaterialDto>>
{
    /// <summary>Verilirse yalnızca Code değeri buna eşit kayıtlar döner (<c>?code=...</c>).</summary>
    public string? Code { get; set; }

    /// <summary>Verilirse yalnızca Unit değeri buna eşit kayıtlar döner (<c>?unit=...</c>).</summary>
    public MaterialUnit? Unit { get; set; }

    /// <summary>Verilirse yalnızca IsActive değeri buna eşit kayıtlar döner (<c>?isactive=...</c>).</summary>
    public bool? IsActive { get; set; }
}

internal sealed class ListMaterialHandler : IQueryHandler<ListMaterialQuery, PagedResult<MaterialDto>>
{
    private readonly IRepository<Material> _repository;

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["Code", "Name", "Unit", "KgPerUnit", "ErpStockCode", "IsActive", "CreatedAt", "UpdatedAt"];

    public ListMaterialHandler(IRepository<Material> repository) => _repository = repository;

    public async Task<PagedResult<MaterialDto>> Handle(ListMaterialQuery request, CancellationToken cancellationToken)
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

                if (request.Unit is { } filterUnit)
                {
                    query = query.Where(x => x.Unit == filterUnit);
                }

                if (request.IsActive is { } filterIsActive)
                {
                    query = query.Where(x => x.IsActive == filterIsActive);
                }

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike(x.Code, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike(x.Name, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.ErpStockCode ?? string.Empty), searchPattern, LikePattern.EscapeCharacter));
            },
            cancellationToken);

        return new PagedResult<MaterialDto>
        {
            Items = items.Select(MaterialDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
