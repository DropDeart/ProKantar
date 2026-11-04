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

    /// <summary>SortBy beyaz listesi (bkz. docs/ARCH.md §6.2).</summary>
    private static readonly string[] SortFields = ["Action", "FieldName", "OldValue", "NewValue", "Reason", "WeighingId", "CreatedAt", "UpdatedAt"];

    public ListWeighingAuditLogHandler(IRepository<WeighingAuditLog> repository) => _repository = repository;

    public async Task<PagedResult<WeighingAuditLogDto>> Handle(ListWeighingAuditLogQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var searchPattern = LikePattern.ContainsOrNull(request.Search) ?? string.Empty;
        var (items, totalCount) = await _repository.ListPagedAsync(
            request.Skip,
            request.PageSize,
            SortExpression.Normalize(request.SortBy, SortFields),
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

                return searchPattern.Length == 0 ? query : query.Where(x => EF.Functions.ILike((x.FieldName ?? string.Empty), searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.OldValue ?? string.Empty), searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.NewValue ?? string.Empty), searchPattern, LikePattern.EscapeCharacter) || EF.Functions.ILike((x.Reason ?? string.Empty), searchPattern, LikePattern.EscapeCharacter));
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
