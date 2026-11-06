using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Shipments;

/// <summary>Sevkiyatın durak yük satırları, durak ve satır sırasıyla.</summary>
public sealed class ShipmentLoadsQuery : IQuery<IReadOnlyList<ShipmentStopLineDto>>
{
    /// <summary>Sevkiyat kimliği.</summary>
    public Guid ShipmentId { get; set; }
}

/// <summary>Durak yük satırı.</summary>
public sealed record ShipmentStopLineDto(
    Guid Id,
    Guid ShipmentStopId,
    int Sequence,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    decimal PlannedQuantity,
    MaterialUnit Unit);

internal sealed class ShipmentLoadsHandler(KantarDbContext db) : IQueryHandler<ShipmentLoadsQuery, IReadOnlyList<ShipmentStopLineDto>>
{
    public async Task<IReadOnlyList<ShipmentStopLineDto>> Handle(ShipmentLoadsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lines = await db.ShipmentStopLines.AsNoTracking()
            .Where(l => l.ShipmentStop!.ShipmentId == request.ShipmentId)
            .OrderBy(l => l.ShipmentStop!.Sequence).ThenBy(l => l.Sequence)
            .Select(l => new ShipmentStopLineDto(
                l.Id, l.ShipmentStopId, l.Sequence, l.MaterialId, l.MaterialCode, l.MaterialName, l.PlannedQuantity, l.Unit))
            .ToListAsync(cancellationToken);

        // Boş sonuçta sevkiyatın kendisi yok mu, yoksa yükü mü yok: yalnız o zaman ayrıca bakılır.
        if (lines.Count == 0 && !await db.Shipments.AnyAsync(s => s.Id == request.ShipmentId, cancellationToken))
        {
            throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        }

        return lines;
    }
}
