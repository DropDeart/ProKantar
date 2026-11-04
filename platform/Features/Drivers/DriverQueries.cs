using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Platform.Entities;
using Microsoft.EntityFrameworkCore;

namespace Platform.Features.Drivers;

/// <summary>Kimliğe göre tek bir Driver getirir.</summary>
public sealed class GetDriverByIdQuery : IQuery<DriverDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Doluysa yalnızca UserId'ı bu kullanıcı olan kayıt döner (başkasınınki bulunamadı sayılır).
    /// Controller doldurur; gRPC gibi servisler arası çağrılarda boş kalır (bkz. docs/ARCH.md §6.1).
    /// </summary>
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class GetDriverByIdHandler : IQueryHandler<GetDriverByIdQuery, DriverDto?>
{
    private readonly IRepository<Driver> _repository;

    public GetDriverByIdHandler(IRepository<Driver> repository) => _repository = repository;

    public async Task<DriverDto?> Handle(GetDriverByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (request.RestrictToOwnerId is { } ownerId && entity.UserId != ownerId)
        {
            return null;
        }

        return DriverDto.From(entity);
    }
}

/// <summary>Driver kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListDriverQuery : PagedRequest, IQuery<PagedResult<DriverDto>>
{
    /// <summary>Verilirse yalnızca CarrierId değeri buna eşit kayıtlar döner (<c>?carrierid=...</c>).</summary>
    public Guid? CarrierId { get; set; }

    /// <summary>Verilirse yalnızca Phone değeri buna eşit kayıtlar döner (<c>?phone=...</c>).</summary>
    public string? Phone { get; set; }

    /// <summary>Doluysa yalnızca UserId'ı bu kullanıcı olan kayıtlar döner. İstemciden bağlanmaz; controller doldurur.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class ListDriverHandler : IQueryHandler<ListDriverQuery, PagedResult<DriverDto>>
{
    private readonly IRepository<Driver> _repository;

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["UserId", "FullName", "Phone", "NationalId", "LicenseNo", "KvkkConsentAt", "LocationConsent", "IsActive", "CarrierId", "CreatedAt", "UpdatedAt"];

    public ListDriverHandler(IRepository<Driver> repository) => _repository = repository;

    public async Task<PagedResult<DriverDto>> Handle(ListDriverQuery request, CancellationToken cancellationToken)
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
                    query = query.Where(x => x.UserId == ownerId);
                }

                if (request.CarrierId is { } filterCarrierId)
                {
                    query = query.Where(x => x.CarrierId == filterCarrierId);
                }

                if (request.Phone is { } filterPhone)
                {
                    query = query.Where(x => x.Phone == filterPhone);
                }

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike(x.FullName, searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike(x.Phone, searchPattern, LikePattern.EscapeCharacter));
            },
            cancellationToken);

        return new PagedResult<DriverDto>
        {
            Items = items.Select(DriverDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
