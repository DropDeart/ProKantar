using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Features.WeighingAuditLogs;

/// <summary>Kimliğe göre tek bir WeighingAuditLog getirir.</summary>
public sealed class GetWeighingAuditLogByIdQuery : IQuery<WeighingAuditLogDto?>
{
    /// <summary>Aranan kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetWeighingAuditLogByIdHandler : IQueryHandler<GetWeighingAuditLogByIdQuery, WeighingAuditLogDto?>
{
    private readonly IRepository<WeighingAuditLog> _repository;

    public GetWeighingAuditLogByIdHandler(IRepository<WeighingAuditLog> repository) => _repository = repository;

    public async Task<WeighingAuditLogDto?> Handle(GetWeighingAuditLogByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return entity is null ? null : WeighingAuditLogDto.From(entity);
    }
}

/// <summary>WeighingAuditLog kayıtlarını sayfalı, sıralı ve aranabilir biçimde listeler.</summary>
public sealed class ListWeighingAuditLogQuery : PagedRequest, IQuery<PagedResult<WeighingAuditLogDto>>
{
    /// <summary>Verilirse yalnızca WeighingId değeri buna eşit kayıtlar döner (<c>?weighingid=...</c>).</summary>
    public Guid? WeighingId { get; set; }

    /// <summary>Verilirse yalnızca Action değeri buna eşit kayıtlar döner (<c>?action=...</c>).</summary>
    public WeighingAuditLogAction? Action { get; set; }
}

internal sealed class ListWeighingAuditLogHandler : IQueryHandler<ListWeighingAuditLogQuery, PagedResult<WeighingAuditLogDto>>
{
    private readonly IRepository<WeighingAuditLog> _repository;

    public ListWeighingAuditLogHandler(IRepository<WeighingAuditLog> repository) => _repository = repository;

    public async Task<PagedResult<WeighingAuditLogDto>> Handle(ListWeighingAuditLogQuery request, CancellationToken cancellationToken)
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

                if (request.Action is { } filterAction)
                {
                    query = query.Where(x => x.Action == filterAction);
                }

                return string.IsNullOrWhiteSpace(request.Search) ? query : query.Where(x => EF.Functions.ILike((x.FieldName ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike((x.OldValue ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike((x.NewValue ?? string.Empty), $"%{request.Search}%") || EF.Functions.ILike((x.Reason ?? string.Empty), $"%{request.Search}%"));
            },
            cancellationToken);

        return new PagedResult<WeighingAuditLogDto>
        {
            Items = items.Select(WeighingAuditLogDto.From).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
