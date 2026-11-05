using BaseForge.Core.CQRS;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Shipments;

/// <summary>Sevkiyat listesi: başlık bilgileri + güzergah özeti (ilk/son durak, durak sayısı), en yeni plan önce.</summary>
public sealed class ShipmentBoardQuery : PagedRequest, IQuery<PagedResult<ShipmentBoardItemDto>>
{
    /// <summary>Durum filtresi.</summary>
    public ShipmentStatus? Status { get; set; }

    /// <summary>Yön filtresi.</summary>
    public ShipmentDirection? Direction { get; set; }
}

/// <summary>Sevkiyat listesi satırı.</summary>
public sealed record ShipmentBoardItemDto(
    Guid Id,
    string ShipmentNo,
    ShipmentDirection Direction,
    ShipmentStatus Status,
    DateTimeOffset PlannedAt,
    string Plate,
    string? DriverName,
    Guid StationId,
    int StopCount,
    string? FirstStop,
    string? LastStop);

internal sealed class ShipmentBoardHandler(KantarDbContext db) : IQueryHandler<ShipmentBoardQuery, PagedResult<ShipmentBoardItemDto>>
{
    public async Task<PagedResult<ShipmentBoardItemDto>> Handle(ShipmentBoardQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var query = db.Shipments.AsNoTracking();
        if (request.Status is { } status) query = query.Where(s => s.Status == status);
        if (request.Direction is { } direction) query = query.Where(s => s.Direction == direction);
        if (LikePattern.ContainsOrNull(request.Search) is { } pattern)
        {
            query = query.Where(s =>
                EF.Functions.ILike(s.ShipmentNo, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike(s.Plate, pattern, LikePattern.EscapeCharacter)
                || (s.DriverName != null && EF.Functions.ILike(s.DriverName, pattern, LikePattern.EscapeCharacter)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.PlannedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(s => new ShipmentBoardItemDto(
                s.Id, s.ShipmentNo, s.Direction, s.Status, s.PlannedAt, s.Plate, s.DriverName, s.StationId,
                db.ShipmentStops.Count(t => t.ShipmentId == s.Id),
                db.ShipmentStops.Where(t => t.ShipmentId == s.Id).OrderBy(t => t.Sequence).Select(t => t.Name).FirstOrDefault(),
                db.ShipmentStops.Where(t => t.ShipmentId == s.Id).OrderByDescending(t => t.Sequence).Select(t => t.Name).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<ShipmentBoardItemDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }
}
