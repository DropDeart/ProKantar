using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Platform.Entities;
using Microsoft.EntityFrameworkCore;

namespace Platform.Features.Carriers;

/// <summary>Kimliğe göre tek bir Carrier getirir.</summary>
public sealed class GetCarrierByIdQuery : IQuery<CarrierDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıt döner (başkasınınki bulunamadı sayılır).
    /// Controller doldurur; gRPC gibi servisler arası çağrılarda boş kalır (bkz. docs/ARCH.md §6.1).
    /// </summary>
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class GetCarrierByIdHandler : IQueryHandler<GetCarrierByIdQuery, CarrierDto?>
{
    private readonly IRepository<Carrier> _repository;

    public GetCarrierByIdHandler(IRepository<Carrier> repository) => _repository = repository;

    public async Task<CarrierDto?> Handle(GetCarrierByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (request.RestrictToOwnerId is { } ownerId && entity.OwnerUserId != ownerId)
        {
            return null;
        }

        return CarrierDto.From(entity);
    }
}

/// <summary>Carrier kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListCarrierQuery : PagedRequest, IQuery<PagedResult<CarrierDto>>
{
    /// <summary>Verilirse yalnızca TaxNo değeri buna eşit kayıtlar döner (<c>?taxno=...</c>).</summary>
    public string? TaxNo { get; set; }

    /// <summary>Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıtlar döner. İstemciden bağlanmaz; controller doldurur.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class ListCarrierHandler : IQueryHandler<ListCarrierQuery, PagedResult<CarrierDto>>
{
    private readonly IRepository<Carrier> _repository;

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["OwnerUserId", "Name", "TaxNo", "TaxOffice", "Phone", "Address", "CreatedAt", "UpdatedAt"];

    public ListCarrierHandler(IRepository<Carrier> repository) => _repository = repository;

    public async Task<PagedResult<CarrierDto>> Handle(ListCarrierQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var searchPattern = LikePattern.ContainsOrNull(request.Search) ?? string.Empty;
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            SortExpression.Normalize(request.SortBy, SortFields),
            query =>
            {
                if (request.RestrictToOwnerId is { } ownerId)
                {
                    query = query.Where(x => x.OwnerUserId == ownerId);
                }

                if (request.TaxNo is { } filterTaxNo)
                {
                    query = query.Where(x => x.TaxNo == filterTaxNo);
                }

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike(x.Name, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.TaxNo ?? string.Empty), searchPattern, LikePattern.EscapeCharacter));
            },
            cancellationToken);

        return new PagedResult<CarrierDto>
        {
            Items = items.Select(CarrierDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
