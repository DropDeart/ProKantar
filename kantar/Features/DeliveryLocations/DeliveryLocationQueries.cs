using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.DeliveryLocations;

/// <summary>Kimliğe göre tek bir DeliveryLocation getirir.</summary>
public sealed class GetDeliveryLocationByIdQuery : IQuery<DeliveryLocationDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetDeliveryLocationByIdHandler : IQueryHandler<GetDeliveryLocationByIdQuery, DeliveryLocationDto?>
{
    private readonly IRepository<DeliveryLocation> _repository;

    public GetDeliveryLocationByIdHandler(IRepository<DeliveryLocation> repository) => _repository = repository;

    public async Task<DeliveryLocationDto?> Handle(GetDeliveryLocationByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : DeliveryLocationDto.From(entity);
    }
}

/// <summary>DeliveryLocation kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListDeliveryLocationQuery : PagedRequest, IQuery<PagedResult<DeliveryLocationDto>>
{
    /// <summary>Verilirse yalnızca CompanyId değeri buna eşit kayıtlar döner (<c>?companyid=...</c>).</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Verilirse yalnızca IsActive değeri buna eşit kayıtlar döner (<c>?isactive=...</c>).</summary>
    public bool? IsActive { get; set; }
}

internal sealed class ListDeliveryLocationHandler : IQueryHandler<ListDeliveryLocationQuery, PagedResult<DeliveryLocationDto>>
{
    private readonly IRepository<DeliveryLocation> _repository;

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["Name", "Address", "Latitude", "Longitude", "RadiusM", "ContactName", "ContactPhone", "IsActive", "CompanyId", "CreatedAt", "UpdatedAt"];

    public ListDeliveryLocationHandler(IRepository<DeliveryLocation> repository) => _repository = repository;

    public async Task<PagedResult<DeliveryLocationDto>> Handle(ListDeliveryLocationQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var searchPattern = LikePattern.ContainsOrNull(request.Search) ?? string.Empty;
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            SortExpression.Normalize(request.SortBy, SortFields),
            query =>
            {
                if (request.CompanyId is { } filterCompanyId)
                {
                    query = query.Where(x => x.CompanyId == filterCompanyId);
                }

                if (request.IsActive is { } filterIsActive)
                {
                    query = query.Where(x => x.IsActive == filterIsActive);
                }

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike(x.Name, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.Address ?? string.Empty), searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.ContactName ?? string.Empty), searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.ContactPhone ?? string.Empty), searchPattern, LikePattern.EscapeCharacter));
            },
            cancellationToken);

        return new PagedResult<DeliveryLocationDto>
        {
            Items = items.Select(DeliveryLocationDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
