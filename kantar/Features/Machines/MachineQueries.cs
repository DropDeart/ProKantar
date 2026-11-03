using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.Machines;

/// <summary>Kimliğe göre tek bir Machine getirir.</summary>
public sealed class GetMachineByIdQuery : IQuery<MachineDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetMachineByIdHandler : IQueryHandler<GetMachineByIdQuery, MachineDto?>
{
    private readonly IRepository<Machine> _repository;

    public GetMachineByIdHandler(IRepository<Machine> repository) => _repository = repository;

    public async Task<MachineDto?> Handle(GetMachineByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : MachineDto.From(entity);
    }
}

/// <summary>Machine kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListMachineQuery : PagedRequest, IQuery<PagedResult<MachineDto>>
{
    /// <summary>Verilirse yalnızca StationId değeri buna eşit kayıtlar döner (<c>?stationid=...</c>).</summary>
    public Guid? StationId { get; set; }

    /// <summary>Verilirse yalnızca IsActive değeri buna eşit kayıtlar döner (<c>?isactive=...</c>).</summary>
    public bool? IsActive { get; set; }
}

internal sealed class ListMachineHandler : IQueryHandler<ListMachineQuery, PagedResult<MachineDto>>
{
    private readonly IRepository<Machine> _repository;

    public ListMachineHandler(IRepository<Machine> repository) => _repository = repository;

    public async Task<PagedResult<MachineDto>> Handle(ListMachineQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            request.SortBy,
            query =>
            {
                if (request.StationId is { } filterStationId)
                {
                    query = query.Where(x => x.StationId == filterStationId);
                }

                if (request.IsActive is { } filterIsActive)
                {
                    query = query.Where(x => x.IsActive == filterIsActive);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike(x.Name, $"%{request.Search}%") || EF.Functions.ILike(x.MachineType, $"%{request.Search}%") || EF.Functions.ILike((x.PlateOrSerial ?? string.Empty), $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<MachineDto>
        {
            Items = items.Select(MachineDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
