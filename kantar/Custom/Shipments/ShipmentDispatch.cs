using BaseForge.Core.Exceptions;
using BaseForge.Core.Messaging;
using Kantar.Entities;

namespace Kantar.Custom.Shipments;

/// <summary>
/// Sevkiyatı doğrulayıp <see cref="ShipmentStatus.Dispatched"/> durumuna geçirir ve
/// <see cref="ShipmentDispatchedEvent"/>'i outbox'a yazar. Kaydetmez; çağıran SaveChanges yapar.
/// <c>DispatchShipment</c> ve plansız çıkışta <c>CompleteWeighing</c> ortak kullanır.
/// </summary>
internal static class ShipmentDispatch
{
    private const string StopsField = nameof(ShipmentDispatchedEvent.Stops);

    /// <summary>Doğrular, durumu günceller ve olayı yayınlar.</summary>
    /// <param name="shipment">Takip edilen (tracked) sevkiyat; yeni eklenmiş olabilir.</param>
    /// <param name="stops">Sevkiyatın durakları (sırasız olabilir; henüz kaydedilmemiş olanlar dahil).</param>
    /// <param name="tenantId">
    /// İsteğin kiracısı. Entity'deki TenantId'ye güvenilmez: yeni eklenen kayıtta SaveChanges'e kadar boştur.
    /// </param>
    public static async Task DispatchAsync(
        Shipment shipment, IReadOnlyCollection<ShipmentStop> stops, Guid tenantId,
        IEventBus eventBus, CancellationToken cancellationToken)
    {
        // Yayından sonra plan değişmediğinden (durum kuralları) yeniden yayın gerekmez; tekrar yayın reddedilir.
        if (shipment.Status != ShipmentStatus.Draft)
        {
            throw new ValidationException(nameof(Shipment.Status), "Yalnızca taslak sevkiyat yayınlanabilir.");
        }

        if (shipment.DriverId is not { } driverId)
        {
            throw new ValidationException(nameof(Shipment.DriverId), "Şoför atanmadan sevkiyat yayınlanamaz.");
        }

        ValidateStops(shipment.Direction, stops);

        shipment.Status = ShipmentStatus.Dispatched;
        shipment.DispatchedAt = DateTimeOffset.UtcNow;

        await eventBus.PublishAsync(new ShipmentDispatchedEvent
        {
            TenantId = tenantId,
            ShipmentId = shipment.Id,
            ShipmentNo = shipment.ShipmentNo,
            Direction = shipment.Direction,
            PlannedAt = shipment.PlannedAt,
            StationId = shipment.StationId,
            VehicleId = shipment.VehicleId,
            Plate = shipment.Plate,
            DriverId = driverId,
            DriverName = shipment.DriverName,
            Stops = stops.OrderBy(s => s.Sequence).Select(ShipmentStopPayload.From).ToList(),
        }, cancellationToken);
    }

    /// <summary>WGS84 aralığında ve (0,0) olmayan koordinat (boş bırakılmış alan genelde 0,0 gelir).</summary>
    private static bool HasValidCoordinate(ShipmentStop stop)
        => stop.Latitude is >= -90 and <= 90
           && stop.Longitude is >= -180 and <= 180
           && !(stop.Latitude == 0 && stop.Longitude == 0);

    private static void ValidateStops(ShipmentDirection direction, IReadOnlyCollection<ShipmentStop> stops)
    {
        if (stops.Count == 0)
        {
            throw new ValidationException(StopsField, "Sevkiyatın en az bir durağı olmalı.");
        }

        if (stops.Select(s => s.Sequence).Distinct().Count() != stops.Count)
        {
            throw new ValidationException(StopsField, "Durak sıraları (Sequence) tekrar edemez.");
        }

        if (stops.FirstOrDefault(s => !HasValidCoordinate(s)) is { } invalid)
        {
            throw new ValidationException(StopsField, $"{invalid.Sequence}. durağın koordinatı geçersiz.");
        }

        var requiredType = direction == ShipmentDirection.Outbound
            ? ShipmentStopStopType.Delivery
            : ShipmentStopStopType.Weighbridge;
        if (stops.All(s => s.StopType != requiredType))
        {
            throw new ValidationException(StopsField, $"{direction} sevkiyatın en az bir '{requiredType}' durağı olmalı.");
        }
    }
}
