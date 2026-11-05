using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Shipments;

/// <summary>
/// Sevkiyat planını (başlık + duraklar) tek işlemde kaydeder. Id yoksa taslak açar, varsa yalnız taslağı
/// günceller. Durak adı ve koordinatı sunucuda teslimat noktası/istasyondan kopyalanır (snapshot).
/// </summary>
public sealed class SaveShipmentPlanCommand : ICommand<Guid>
{
    /// <summary>Güncellenecek taslak (route'tan); yeni plan için boş.</summary>
    public Guid? Id { get; set; }

    /// <summary>Boşsa sunucu üretir: <c>{istasyon kodu}-S{yyMMdd}-{sıra}</c> (gün Türkiye saatine göre).</summary>
    public string? ShipmentNo { get; set; }

    /// <summary>Çıkış = teslimata giden araç; giriş = kantara gelen araç randevusu.</summary>
    public ShipmentDirection Direction { get; set; }

    /// <summary>Planlanan çıkış/varış zamanı.</summary>
    public DateTimeOffset PlannedAt { get; set; }

    /// <summary>Sevkiyatın bağlı olduğu istasyon.</summary>
    public Guid StationId { get; set; }

    /// <summary>platform/Vehicle kimliği.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Plaka (snapshot).</summary>
    public string Plate { get; set; } = string.Empty;

    /// <summary>platform/Driver kimliği; yayınlamak için zorunlu, taslakta boş olabilir.</summary>
    public Guid? DriverId { get; set; }

    /// <summary>Şoför adı (snapshot).</summary>
    public string? DriverName { get; set; }

    /// <summary>Not.</summary>
    public string? Note { get; set; }

    /// <summary>Duraklar, sırasıyla.</summary>
    public IReadOnlyList<ShipmentPlanStop> Stops { get; set; } = [];
}

/// <summary>Plan durağı: teslimat/yükleme noktası ya da kantar istasyonu.</summary>
public sealed class ShipmentPlanStop
{
    /// <summary>Durak türü.</summary>
    public ShipmentStopStopType StopType { get; set; }

    /// <summary>Teslimat ya da yükleme noktası (Delivery/Pickup).</summary>
    public Guid? DeliveryLocationId { get; set; }

    /// <summary>Kantar istasyonu (Weighbridge).</summary>
    public Guid? StationId { get; set; }

    /// <summary>Planlanan varış.</summary>
    public DateTimeOffset? PlannedAt { get; set; }

    /// <summary>Durak notu.</summary>
    public string? Note { get; set; }
}

internal sealed class SaveShipmentPlanHandler(KantarDbContext db, IUnitOfWork unitOfWork, ICurrentTenant currentTenant)
    : ICommandHandler<SaveShipmentPlanCommand, Guid>
{
    private const string StopsField = nameof(SaveShipmentPlanCommand.Stops);

    public async Task<Guid> Handle(SaveShipmentPlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Plate))
        {
            throw new ValidationException(nameof(request.Plate), "Araç plakası zorunlu.");
        }

        if (request.Stops.Count == 0)
        {
            throw new ValidationException(StopsField, "En az bir durak ekleyin.");
        }

        var station = await db.Stations.AsNoTracking()
            .Where(s => s.Id == request.StationId)
            .Select(s => new { s.Code })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Station), request.StationId);

        var tenantId = currentTenant.RequireTenantId();
        var manualNo = string.IsNullOrWhiteSpace(request.ShipmentNo) ? null : request.ShipmentNo.Trim();

        // Numara ayırma kilidi commit'e kadar tutulur; eş zamanlı iki plan aynı numarayı alamaz.
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var shipment = request.Id is { } id ? await LoadDraftAsync(id, cancellationToken) : null;
        await ShipmentNumbers.RequireUniqueAsync(db, tenantId, manualNo, shipment?.Id, cancellationToken);
        if (shipment is null)
        {
            shipment = new Shipment
            {
                ShipmentNo = manualNo
                    ?? await ShipmentNumbers.AllocateAsync(db, tenantId, ShipmentNumbers.PrefixFor(station.Code, request.PlannedAt), cancellationToken),
            };
            db.Shipments.Add(shipment);
        }
        else
        {
            db.ShipmentStops.RemoveRange(await db.ShipmentStops.Where(s => s.ShipmentId == shipment.Id).ToListAsync(cancellationToken));
            if (manualNo is not null)
            {
                shipment.ShipmentNo = manualNo;
            }
        }

        shipment.Direction = request.Direction;
        shipment.PlannedAt = request.PlannedAt;
        shipment.StationId = request.StationId;
        shipment.VehicleId = request.VehicleId;
        shipment.Plate = request.Plate.Trim().ToUpperInvariant();
        shipment.DriverId = request.DriverId;
        shipment.DriverName = request.DriverName?.Trim();
        shipment.Note = request.Note?.Trim();

        var sequence = 0;
        foreach (var plan in request.Stops)
        {
            db.ShipmentStops.Add(await ToStopAsync(shipment.Id, ++sequence, plan, cancellationToken));
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ShipmentNumbers.IsDuplicate(ex))
        {
            throw new ValidationException(ShipmentNumbers.Field, $"{shipment.ShipmentNo} numaralı sevkiyat zaten var.");
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return shipment.Id;
    }

    private async Task<Shipment> LoadDraftAsync(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), id);
        return shipment.Status == ShipmentStatus.Draft
            ? shipment
            : throw new ValidationException(nameof(Shipment.Status), "Yalnız taslak sevkiyat düzenlenebilir.");
    }

    private async Task<ShipmentStop> ToStopAsync(Guid shipmentId, int sequence, ShipmentPlanStop plan, CancellationToken cancellationToken)
    {
        var stop = new ShipmentStop
        {
            ShipmentId = shipmentId,
            Sequence = sequence,
            StopType = plan.StopType,
            PlannedAt = plan.PlannedAt,
            Note = plan.Note?.Trim(),
        };

        if (plan.StopType == ShipmentStopStopType.Weighbridge)
        {
            var stationId = plan.StationId ?? throw new ValidationException(StopsField, $"{sequence}. durak için istasyon seçin.");
            var station = await db.Stations.AsNoTracking()
                .Where(s => s.Id == stationId)
                .Select(s => new { s.Name, s.Latitude, s.Longitude, s.RadiusM })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(nameof(Station), stationId);
            if (station.Latitude is not { } lat || station.Longitude is not { } lng)
            {
                throw new ValidationException(StopsField, $"{station.Name} istasyonunun konumu tanımlı değil; Tanımlar'dan haritada işaretleyin.");
            }

            (stop.StationId, stop.Name, stop.Latitude, stop.Longitude, stop.RadiusM) = (stationId, station.Name, lat, lng, station.RadiusM);
            return stop;
        }

        var locationId = plan.DeliveryLocationId ?? throw new ValidationException(StopsField, $"{sequence}. durak için teslimat noktası seçin.");
        var location = await db.DeliveryLocations.AsNoTracking()
            .Where(l => l.Id == locationId)
            .Select(l => new { l.Name, l.Latitude, l.Longitude, l.RadiusM })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(DeliveryLocation), locationId);

        (stop.DeliveryLocationId, stop.Name, stop.Latitude, stop.Longitude, stop.RadiusM) =
            (locationId, location.Name, location.Latitude, location.Longitude, location.RadiusM);
        return stop;
    }
}
