using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using BaseForge.Core.Messaging;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Shipments;

/// <summary>Taslak sevkiyat planını yayınlar; tracking seferi açar.</summary>
public sealed class DispatchShipmentCommand : ICommand
{
    /// <summary>Sevkiyat kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DispatchShipmentHandler(
    KantarDbContext db, IUnitOfWork unitOfWork, IEventBus eventBus, ICurrentTenant currentTenant)
    : ICommandHandler<DispatchShipmentCommand>
{
    public async Task Handle(DispatchShipmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.Id);
        var stops = await db.ShipmentStops.Where(s => s.ShipmentId == shipment.Id).ToListAsync(cancellationToken);
        await RequireLoadsAsync(shipment, stops, cancellationToken);

        await ShipmentDispatch.DispatchAsync(shipment, stops, currentTenant.RequireTenantId(), eventBus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Kantar malzemeyi plandan aldığı için çıkış sevkiyatında her teslimat durağının yükü olmalı ve toplam yük
    /// kantar kapasitesini aşmamalı.
    /// </summary>
    private async Task RequireLoadsAsync(Shipment shipment, IReadOnlyCollection<ShipmentStop> stops, CancellationToken cancellationToken)
    {
        if (shipment.Direction != ShipmentDirection.Outbound)
        {
            return;
        }

        var stopIds = stops.Select(s => s.Id).ToList();
        var lines = await db.ShipmentStopLines.AsNoTracking()
            .Where(l => stopIds.Contains(l.ShipmentStopId))
            .Include(l => l.Material)
            .ToListAsync(cancellationToken);
        var loaded = lines.Select(l => l.ShipmentStopId).ToHashSet();
        var empty = stops.Where(s => s.StopType == ShipmentStopStopType.Delivery && !loaded.Contains(s.Id))
            .OrderBy(s => s.Sequence)
            .FirstOrDefault();
        if (empty is not null)
        {
            throw new ValidationException("Stops", $"{empty.Sequence}. durağa ({empty.Name}) yük eklenmedi; yayınlamadan önce malzeme ve miktar girin.");
        }

        // Taslak kaydından sonra kantar kapasitesi düşürülmüş olabilir; yayında yeniden denetlenir.
        await ShipmentLoadCapacity.RequireWithinAsync(db, stops, lines, l => l.Material, cancellationToken);
    }
}
