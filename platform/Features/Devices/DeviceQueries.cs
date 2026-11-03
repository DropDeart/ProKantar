using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Platform.Entities;
using Microsoft.EntityFrameworkCore;

namespace Platform.Features.Devices;

/// <summary>Kimliğe göre tek bir Device getirir.</summary>
public sealed class GetDeviceByIdQuery : IQuery<DeviceDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Doluysa yalnızca UserId'ı bu kullanıcı olan kayıt döner (başkasınınki bulunamadı sayılır).
    /// Controller doldurur; gRPC gibi servisler arası çağrılarda boş kalır (bkz. docs/ARCH.md §6.1).
    /// </summary>
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class GetDeviceByIdHandler : IQueryHandler<GetDeviceByIdQuery, DeviceDto?>
{
    private readonly IRepository<Device> _repository;

    public GetDeviceByIdHandler(IRepository<Device> repository) => _repository = repository;

    public async Task<DeviceDto?> Handle(GetDeviceByIdQuery request, CancellationToken cancellationToken)
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

        return DeviceDto.From(entity);
    }
}

/// <summary>Device kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListDeviceQuery : PagedRequest, IQuery<PagedResult<DeviceDto>>
{
    /// <summary>Verilirse yalnızca DriverId değeri buna eşit kayıtlar döner (<c>?driverid=...</c>).</summary>
    public Guid? DriverId { get; set; }

    /// <summary>Doluysa yalnızca UserId'ı bu kullanıcı olan kayıtlar döner. İstemciden bağlanmaz; controller doldurur.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class ListDeviceHandler : IQueryHandler<ListDeviceQuery, PagedResult<DeviceDto>>
{
    private readonly IRepository<Device> _repository;

    public ListDeviceHandler(IRepository<Device> repository) => _repository = repository;

    public async Task<PagedResult<DeviceDto>> Handle(ListDeviceQuery request, CancellationToken cancellationToken)
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
                    query = query.Where(x => x.UserId == ownerId);
                }

                if (request.DriverId is { } filterDriverId)
                {
                    query = query.Where(x => x.DriverId == filterDriverId);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike(x.DeviceIdentifier, $"%{request.Search}%") || EF.Functions.ILike((x.PushToken ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike((x.AppVersion ?? string.Empty), $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<DeviceDto>
        {
            Items = items.Select(DeviceDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
