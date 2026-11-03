using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using BaseForge.Core.Messaging;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Shipments;

/// <summary>Taslak (veya yeniden yayınlanacak) sevkiyat planını yayınlar; tracking seferi açar.</summary>
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

        await ShipmentDispatch.DispatchAsync(shipment, stops, currentTenant.RequireTenantId(), eventBus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
