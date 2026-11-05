using System.Globalization;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using BaseForge.Core.Messaging;
using Kantar.Custom.Shipments;
using Kantar.Data;
using Kantar.Entities;
using Kantar.Features.Weighings;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Weighings;

/// <summary>
/// Açık tartımı ikinci tartımla (veya kayıtlı darayla) kapatır: brüt/dara/net sunucuda hesaplanır,
/// elle girilen ağırlık audit'e yazılır. Plansız çıkışta teslimat lokasyonu verilirse tek duraklı
/// sevkiyat açılıp yayınlanır.
/// </summary>
public sealed class CompleteWeighingCommand : ICommand
{
    /// <summary>Tartım kimliği (route'tan).</summary>
    public Guid Id { get; set; }

    /// <summary>İkinci tartım (kg). <see cref="UseStoredTare"/> true ise verilmez.</summary>
    public decimal? SecondWeightKg { get; set; }

    /// <summary>İkinci tartım farklı bir kantarda yapıldıysa o kantar.</summary>
    public Guid? SecondWeighbridgeId { get; set; }

    /// <summary>İkinci ağırlık elle mi girildi? True ise <see cref="Reason"/> zorunlu.</summary>
    public bool SecondIsManual { get; set; }

    /// <summary>İkinci tartım yerine aracın kayıtlı darası kullanılsın.</summary>
    public bool UseStoredTare { get; set; }

    /// <summary>Elle giriş gerekçesi.</summary>
    public string? Reason { get; set; }

    /// <summary>Plansız çıkışta teslimat noktası; verilirse sevkiyat otomatik açılır.</summary>
    public Guid? DeliveryLocationId { get; set; }
}

internal sealed class CompleteWeighingHandler(
    KantarDbContext db, IUnitOfWork unitOfWork, IEventBus eventBus, ICurrentTenant currentTenant)
    : ICommandHandler<CompleteWeighingCommand>
{
    public async Task Handle(CompleteWeighingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = currentTenant.RequireTenantId();

        // Eş zamanlı iki tamamlama (çift tık, istemci yeniden denemesi) aynı tartımı iki kez kapatmasın:
        // satır kilitlenir, ikinci istek ilki bitince durumu Completed görür.
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Weighings\" WHERE \"Id\" = {request.Id} FOR UPDATE", cancellationToken);

        var weighing = await db.Weighings.FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Weighing), request.Id);

        if (weighing.Status != WeighingStatus.Open)
        {
            throw new ValidationException(nameof(Weighing.Status), "Yalnızca açık tartım tamamlanabilir.");
        }

        if (request.SecondIsManual && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException(nameof(request.Reason), "Elle girilen ağırlık için gerekçe zorunlu.");
        }

        // Malzeme/firma sonradan silinmiş olsa da tartım kapanabilsin: adlar silinmiş kayıtlardan da okunur.
        var materialName = await db.Materials.IgnoreQueryFilters()
            .Where(m => m.Id == weighing.MaterialId && m.TenantId == tenantId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync(cancellationToken);
        var companyName = weighing.CompanyId is { } companyId
            ? await db.Companies.IgnoreQueryFilters()
                .Where(c => c.Id == companyId && c.TenantId == tenantId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var now = DateTimeOffset.UtcNow;
        var netKg = await ApplyWeightsAsync(weighing, request, now, cancellationToken);
        weighing.Status = WeighingStatus.Completed;

        if (request.SecondIsManual)
        {
            db.WeighingAuditLogs.Add(new WeighingAuditLog
            {
                WeighingId = weighing.Id,
                Action = WeighingAuditLogAction.ManualWeight,
                FieldName = nameof(Weighing.SecondWeightKg),
                NewValue = weighing.SecondWeightKg?.ToString(CultureInfo.InvariantCulture),
                Reason = request.Reason,
            });
        }

        if (request.DeliveryLocationId is { } deliveryLocationId)
        {
            await OpenShipmentAsync(weighing, deliveryLocationId, tenantId, now, cancellationToken);
        }

        await eventBus.PublishAsync(new WeighingCompletedEvent
        {
            TenantId = tenantId,
            WeighingId = weighing.Id,
            ShipmentId = weighing.ShipmentId,
            TicketNo = weighing.TicketNo,
            Direction = weighing.Direction,
            StationId = weighing.StationId,
            VehicleId = weighing.VehicleId,
            Plate = weighing.Plate,
            DriverId = weighing.DriverId,
            MaterialId = weighing.MaterialId,
            MaterialName = materialName ?? string.Empty,
            CompanyId = weighing.CompanyId,
            CompanyName = companyName,
            NetKg = netKg,
            CompletedAt = now,
        }, cancellationToken);
        await eventBus.PublishAsync(new WeighingUpdatedEvent { Data = WeighingDto.From(weighing) }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <summary>İkinci tartımı veya kayıtlı darayı uygular; brüt = büyük, dara = küçük ağırlık. Net'i döner.</summary>
    private async Task<decimal> ApplyWeightsAsync(
        Weighing weighing, CompleteWeighingCommand request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        decimal second;
        if (request.UseStoredTare)
        {
            second = await db.TenantVehicles
                .Where(v => v.VehicleId == weighing.VehicleId)
                .Select(v => v.StoredTareKg)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ValidationException(nameof(request.UseStoredTare), "Aracın kayıtlı darası yok.");
            weighing.TareSource = WeighingTareSource.Stored;
        }
        else
        {
            if (request.SecondWeightKg is not { } measured || measured <= 0)
            {
                throw new ValidationException(nameof(request.SecondWeightKg), "İkinci tartım ağırlığı sıfırdan büyük olmalı.");
            }

            second = measured;
            weighing.TareSource = WeighingTareSource.Measured;
            weighing.SecondWeightKg = second;
            weighing.SecondIsManual = request.SecondIsManual;
            weighing.SecondWeighbridgeId = request.SecondWeighbridgeId;
        }

        if (second == weighing.FirstWeightKg)
        {
            throw new ValidationException(nameof(Weighing.NetKg), "Net ağırlık sıfır olamaz; iki tartım aynı.");
        }

        var gross = Math.Max(weighing.FirstWeightKg, second);
        var tare = Math.Min(weighing.FirstWeightKg, second);
        weighing.SecondWeighedAt = now;
        weighing.GrossKg = gross;
        weighing.TareKg = tare;
        weighing.NetKg = gross - tare;
        return gross - tare;
    }

    /// <summary>Plansız çıkış için tek teslimat duraklı sevkiyat açar ve yayınlar.</summary>
    private async Task OpenShipmentAsync(
        Weighing weighing, Guid deliveryLocationId, Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (weighing.Direction != WeighingDirection.Outbound || weighing.ShipmentId is not null)
        {
            throw new ValidationException(nameof(CompleteWeighingCommand.DeliveryLocationId),
                "Teslimat noktası yalnızca sevkiyatı olmayan çıkış tartımında verilebilir.");
        }

        var location = await db.DeliveryLocations
            .Where(l => l.Id == deliveryLocationId)
            .Select(l => new { l.Name, l.Latitude, l.Longitude, l.RadiusM })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(DeliveryLocation), deliveryLocationId);

        // Sevkiyat no fiş no'dur; fiş no istasyon içinde benzersiz olduğundan başka istasyonda aynısı varsa
        // istasyonun sıradaki sevkiyat numarası verilir (kiracı içinde benzersizlik).
        var shipmentNo = weighing.TicketNo;
        if (string.IsNullOrWhiteSpace(shipmentNo) || await ShipmentNumbers.IsTakenAsync(db, tenantId, shipmentNo, null, cancellationToken))
        {
            var stationCode = await db.Stations.IgnoreQueryFilters()
                .Where(s => s.Id == weighing.StationId && s.TenantId == tenantId)
                .Select(s => s.Code)
                .FirstAsync(cancellationToken);
            shipmentNo = await ShipmentNumbers.AllocateAsync(db, tenantId, ShipmentNumbers.PrefixFor(stationCode, now), cancellationToken);
        }

        // Status varsayılanı Draft; TenantId SaveChanges'te BaseForge tarafından damgalanır.
        var shipment = new Shipment
        {
            ShipmentNo = shipmentNo,
            Direction = ShipmentDirection.Outbound,
            PlannedAt = now,
            Plate = weighing.Plate,
            DriverId = weighing.DriverId,
            DriverName = weighing.DriverName,
            StationId = weighing.StationId,
            VehicleId = weighing.VehicleId,
        };
        db.Shipments.Add(shipment);

        var stop = new ShipmentStop
        {
            ShipmentId = shipment.Id,
            Sequence = 1,
            StopType = ShipmentStopStopType.Delivery,
            Name = location.Name,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            RadiusM = location.RadiusM,
            DeliveryLocationId = deliveryLocationId,
        };
        db.ShipmentStops.Add(stop);

        weighing.ShipmentId = shipment.Id;
        await ShipmentDispatch.DispatchAsync(shipment, [stop], tenantId, eventBus, cancellationToken);
    }
}
