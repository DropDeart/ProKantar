using BaseForge.Core.Exceptions;
using Kantar.Custom.EDespatch;
using Kantar.Custom.Shipments;
using Kantar.Data;
using Kantar.Entities;
using Kantar.Features.Weighings;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Weighings;

/// <summary>
/// Kantar sevkiyattan beslenir: kantar yalnız ölçer (plaka, ağırlık, istasyon). Çıkış tartımı plakadan yayındaki
/// sevkiyata bağlanır; malzeme, cari ve şoför plandan doldurulur. Plana bağlı tartımda başka malzeme gönderilemez.
/// </summary>
internal static class WeighingShipmentLink
{
    private static readonly ShipmentStatus[] Active = [ShipmentStatus.Dispatched, ShipmentStatus.InProgress];

    public static async Task ApplyAsync(KantarDbContext db, CreateWeighingCommand request, CancellationToken ct)
    {
        if (request.ShipmentId is null && request.Direction == WeighingDirection.Outbound)
        {
            request.ShipmentId = await FindByPlateAsync(db, request.Plate, request.StationId, ct);
        }

        if (request.ShipmentId is not { } shipmentId)
        {
            if (request.MaterialId == Guid.Empty)
            {
                // Plaka başka istasyondan çıkan yayındaki sevkiyata aitse asıl sorun istasyondur; bunu söyle.
                var elsewhere = request.Direction == WeighingDirection.Outbound
                    ? await FindElsewhereAsync(db, request.Plate, request.StationId, ct)
                    : null;
                throw new ValidationException(nameof(request.MaterialId), elsewhere ??
                    "Bu plakaya bu istasyondan çıkan yayında bir sevkiyat yok. Sevkiyatı planlayıp yayınlayın ya da plansız tartım için malzeme seçin.");
            }

            return;
        }

        var plan = await PlanOfAsync(db, shipmentId, ct);
        if (plan.MainMaterialId is { } planned)
        {
            if (request.MaterialId != Guid.Empty && !plan.MaterialIds.Contains(request.MaterialId))
            {
                throw new ValidationException(nameof(request.MaterialId), "Malzeme sevkiyat planından gelir; plandaki yükü değiştirin.");
            }

            if (request.MaterialId == Guid.Empty)
            {
                request.MaterialId = planned;
            }
        }
        else if (request.MaterialId == Guid.Empty)
        {
            throw new ValidationException(nameof(request.MaterialId), "Sevkiyatın yük satırı yok; malzeme seçin.");
        }

        request.CompanyId ??= plan.CompanyId;
        request.DriverId ??= plan.DriverId;
        request.DriverName ??= plan.DriverName;
    }

    /// <summary>
    /// Plakası tutan, bu istasyondan çıkan (ya da kantar durağı bu istasyonda olan) ve henüz çıkış tartımı olmayan
    /// yayındaki sevkiyat; birden çoksa planlanma zamanı en yakın olan.
    /// </summary>
    private static async Task<Guid?> FindByPlateAsync(KantarDbContext db, string plate, Guid stationId, CancellationToken ct)
    {
        var key = DespatchRules.NormalizePlate(plate);
        if (key.Length == 0)
        {
            return null;
        }

        var candidates = await db.Shipments.AsNoTracking()
            .Where(s => s.Direction == ShipmentDirection.Outbound && Active.Contains(s.Status))
            .Where(s => s.StationId == stationId
                || db.ShipmentStops.Any(st => st.ShipmentId == s.Id && st.StopType == ShipmentStopStopType.Weighbridge && st.StationId == stationId))
            .Where(s => !db.Weighings.Any(w => w.ShipmentId == s.Id && w.Direction == WeighingDirection.Outbound && w.Status != WeighingStatus.Cancelled))
            .Select(s => new { s.Id, s.Plate, s.PlannedAt })
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        return candidates
            .Where(s => DespatchRules.NormalizePlate(s.Plate) == key)
            .OrderBy(s => (s.PlannedAt - now).Duration())
            .Select(s => (Guid?)s.Id)
            .FirstOrDefault();
    }

    /// <summary>Plaka başka istasyondan çıkan yayındaki sevkiyattaysa açıklama; yoksa null.</summary>
    private static async Task<string?> FindElsewhereAsync(KantarDbContext db, string plate, Guid stationId, CancellationToken ct)
    {
        var key = DespatchRules.NormalizePlate(plate);
        var candidates = await db.Shipments.AsNoTracking()
            .Where(s => s.Direction == ShipmentDirection.Outbound && Active.Contains(s.Status) && s.StationId != stationId)
            .Select(s => new { s.ShipmentNo, s.Plate, Station = s.Station!.Name })
            .ToListAsync(ct);
        var match = candidates.FirstOrDefault(s => DespatchRules.NormalizePlate(s.Plate) == key);
        return match is null
            ? null
            : $"{match.Plate} plakalı {match.ShipmentNo} sevkiyatı {match.Station} istasyonundan çıkıyor; o istasyonun kantarında tartın.";
    }

    private sealed record Plan(Guid? MainMaterialId, IReadOnlySet<Guid> MaterialIds, Guid? CompanyId, Guid? DriverId, string? DriverName);

    /// <summary>Ana malzeme: kg karşılığı en büyük satır (çevrilemiyorsa ilk satır).</summary>
    private static async Task<Plan> PlanOfAsync(KantarDbContext db, Guid shipmentId, CancellationToken ct)
    {
        var shipment = await db.Shipments.AsNoTracking()
            .Where(s => s.Id == shipmentId)
            .Select(s => new { s.DriverId, s.DriverName })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Shipment), shipmentId);

        var lines = await db.ShipmentStopLines.AsNoTracking()
            .Where(l => db.ShipmentStops.Any(s => s.Id == l.ShipmentStopId && s.ShipmentId == shipmentId))
            .Include(l => l.Material)
            .OrderBy(l => l.ShipmentStop!.Sequence).ThenBy(l => l.Sequence)
            .ToListAsync(ct);
        var main = lines
            .Select((l, i) => new { l.MaterialId, Kg = l.Material is null ? null : ShipmentLoadCapacity.KgOf(l, l.Material), i })
            .OrderByDescending(x => x.Kg ?? -1m).ThenBy(x => x.i)
            .FirstOrDefault();

        var companyId = await db.ShipmentStops.AsNoTracking()
            .Where(s => s.ShipmentId == shipmentId && s.StopType == ShipmentStopStopType.Delivery && s.DeliveryLocation != null)
            .OrderBy(s => s.Sequence)
            .Select(s => (Guid?)s.DeliveryLocation!.CompanyId)
            .FirstOrDefaultAsync(ct);

        return new Plan(main?.MaterialId, lines.Select(l => l.MaterialId).ToHashSet(), companyId, shipment.DriverId, shipment.DriverName);
    }
}
