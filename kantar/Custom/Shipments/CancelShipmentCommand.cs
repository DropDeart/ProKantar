using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using BaseForge.Core.Messaging;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Shipments;

/// <summary>
/// Taslak, yayınlanmış ya da yoldaki sevkiyatı gerekçeyle iptal eder. Yayınlanmışsa <see cref="ShipmentCancelledEvent"/>
/// ile tracking seferi kapatır ve sevkiyat şoförün uygulamasından düşer.
/// </summary>
public sealed class CancelShipmentCommand : ICommand
{
    /// <summary>Sevkiyat kimliği (route'tan).</summary>
    public Guid Id { get; set; }

    /// <summary>İptal gerekçesi (zorunlu).</summary>
    public string? Reason { get; set; }
}

internal sealed class CancelShipmentHandler(
    KantarDbContext db, IUnitOfWork unitOfWork, IEventBus eventBus, ICurrentTenant currentTenant)
    : ICommandHandler<CancelShipmentCommand>
{
    private const int MaxReasonLength = 250;

    public async Task Handle(CancelShipmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > MaxReasonLength)
        {
            throw new ValidationException(nameof(request.Reason), $"İptal gerekçesi zorunlu (en çok {MaxReasonLength} karakter).");
        }

        // Tracking'den gelen ilerleme/tamamlanma ile yarışmasın: satır kilitlenir, durum kilit altında okunur.
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Shipments\" WHERE \"Id\" = {request.Id} FOR UPDATE", cancellationToken);

        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.Id);
        if (shipment.Status is not (ShipmentStatus.Draft or ShipmentStatus.Dispatched or ShipmentStatus.InProgress))
        {
            throw new ValidationException(nameof(Shipment.Status), "Tamamlanmış ya da iptal edilmiş sevkiyat iptal edilemez.");
        }

        var wasDispatched = shipment.Status is ShipmentStatus.Dispatched or ShipmentStatus.InProgress;
        shipment.Status = ShipmentStatus.Cancelled;
        shipment.CancelReason = reason;

        if (wasDispatched)
        {
            await eventBus.PublishAsync(new ShipmentCancelledEvent
            {
                TenantId = currentTenant.RequireTenantId(),
                ShipmentId = shipment.Id,
                Reason = reason,
                CancelledAt = DateTimeOffset.UtcNow,
            }, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}

/// <summary>Yayınlanmış sevkiyat iptal edildi (tracking seferi kapatır).</summary>
public sealed class ShipmentCancelledEvent : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string EventType => "kantar/ShipmentCancelled";

    /// <summary>Kiracı.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Sevkiyat (= Trip Id).</summary>
    public required Guid ShipmentId { get; init; }

    /// <summary>Gerekçe.</summary>
    public required string Reason { get; init; }

    /// <summary>İptal zamanı.</summary>
    public required DateTimeOffset CancelledAt { get; init; }
}
