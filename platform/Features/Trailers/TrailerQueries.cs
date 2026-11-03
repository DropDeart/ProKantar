using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Platform.Entities;
using Microsoft.EntityFrameworkCore;

namespace Platform.Features.Trailers;

/// <summary>Kimliğe göre tek bir Trailer getirir.</summary>
public sealed class GetTrailerByIdQuery : IQuery<TrailerDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıt döner (başkasınınki bulunamadı sayılır).
    /// Controller doldurur; gRPC gibi servisler arası çağrılarda boş kalır (bkz. docs/ARCH.md §6.1).
    /// </summary>
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class GetTrailerByIdHandler : IQueryHandler<GetTrailerByIdQuery, TrailerDto?>
{
    private readonly IRepository<Trailer> _repository;

    public GetTrailerByIdHandler(IRepository<Trailer> repository) => _repository = repository;

    public async Task<TrailerDto?> Handle(GetTrailerByIdQuery request, CancellationToken cancellationToken)
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

        return TrailerDto.From(entity);
    }
}

/// <summary>Trailer kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListTrailerQuery : PagedRequest, IQuery<PagedResult<TrailerDto>>
{
    /// <summary>Verilirse yalnızca Plate değeri buna eşit kayıtlar döner (<c>?plate=...</c>).</summary>
    public string? Plate { get; set; }

    /// <summary>Verilirse yalnızca CarrierId değeri buna eşit kayıtlar döner (<c>?carrierid=...</c>).</summary>
    public Guid? CarrierId { get; set; }

    /// <summary>Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıtlar döner. İstemciden bağlanmaz; controller doldurur.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class ListTrailerHandler : IQueryHandler<ListTrailerQuery, PagedResult<TrailerDto>>
{
    private readonly IRepository<Trailer> _repository;

    public ListTrailerHandler(IRepository<Trailer> repository) => _repository = repository;

    public async Task<PagedResult<TrailerDto>> Handle(ListTrailerQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            request.SortBy,
            query =>
            {
                if (request.RestrictToOwnerId is { } ownerId)
                {
                    query = query.Where(x => x.OwnerUserId == ownerId);
                }

                if (request.Plate is { } filterPlate)
                {
                    query = query.Where(x => x.Plate == filterPlate);
                }

                if (request.CarrierId is { } filterCarrierId)
                {
                    query = query.Where(x => x.CarrierId == filterCarrierId);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike(x.Plate, $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<TrailerDto>
        {
            Items = items.Select(TrailerDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
