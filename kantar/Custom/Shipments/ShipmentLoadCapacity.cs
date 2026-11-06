using System.Globalization;
using BaseForge.Core.Exceptions;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Shipments;

/// <summary>
/// Planlanan yükün kantar kapasitesine uygunluğu. Araç yükün tamamıyla her kantar durağından geçer; toplam yük
/// (kg) o istasyondaki en büyük aktif kantarın kapasitesini aşamaz.
/// </summary>
internal static class ShipmentLoadCapacity
{
    /// <summary>Satırın kg karşılığı; birim kg'a çevrilemiyorsa (yoğunluk tanımsız) <c>null</c>.</summary>
    public static decimal? KgOf(ShipmentStopLine line, Material material) => line.Unit switch
    {
        MaterialUnit.Kg => line.PlannedQuantity,
        MaterialUnit.Ton => line.PlannedQuantity * 1000m,
        _ when line.Unit == material.Unit && material.KgPerUnit is > 0 => line.PlannedQuantity * material.KgPerUnit.Value,
        _ => null,
    };

    /// <summary>
    /// Satırların toplam yükü (kg) kantar durağı istasyonlarından birinin kapasitesini aşarsa doğrulama hatası atar.
    /// kg'a çevrilemeyen satırlar (yoğunluk tanımsız) toplama katılmaz.
    /// </summary>
    public static async Task RequireWithinAsync(
        KantarDbContext db, IReadOnlyCollection<ShipmentStop> stops, IEnumerable<ShipmentStopLine> lines,
        Func<ShipmentStopLine, Material?> materialOf, CancellationToken cancellationToken)
    {
        var totalKg = lines.Sum(l => materialOf(l) is { } material ? KgOf(l, material) ?? 0m : 0m);
        var stationIds = stops.Where(s => s.StopType == ShipmentStopStopType.Weighbridge && s.StationId is not null)
            .Select(s => s.StationId!.Value)
            .Distinct()
            .ToList();
        if (totalKg <= 0 || stationIds.Count == 0)
        {
            return;
        }

        var limits = await db.Weighbridges.AsNoTracking()
            .Where(w => w.IsActive && stationIds.Contains(w.StationId))
            .GroupBy(w => w.StationId)
            .Select(g => new { StationId = g.Key, CapacityKg = g.Max(w => w.CapacityKg) })
            .ToListAsync(cancellationToken);
        var tightest = limits.Where(l => l.CapacityKg > 0).OrderBy(l => l.CapacityKg).FirstOrDefault();
        if (tightest is not null && totalKg > tightest.CapacityKg)
        {
            var station = stops.First(s => s.StationId == tightest.StationId).Name;
            var tr = CultureInfo.GetCultureInfo("tr-TR");
            throw new ValidationException(
                nameof(SaveShipmentPlanCommand.Stops),
                $"Planlanan yük {totalKg.ToString("N0", tr)} kg; {station} kantarının kapasitesi {tightest.CapacityKg.ToString("N0", tr)} kg. Yükü azaltın ya da sevkiyatı bölün.");
        }
    }
}
