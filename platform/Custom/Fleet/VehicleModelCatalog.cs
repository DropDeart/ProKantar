using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Entities;
using static Platform.Entities.VehicleBodyType;

namespace Platform.Custom.Fleet;

/// <summary>
/// Başlangıç araç kataloğu: Türkiye'de yaygın ağır vasıtalar. Değerler üretici verilerinden derlenmiş tipik
/// değerlerdir (azami ağırlık Türkiye tescilindeki dingil sınırına göre; yakıt tahmini). Açılışta eksik olanlar
/// eklenir, var olanlara (marka + model + yapı) dokunulmaz; düzeltmeler panelden yapılır.
/// </summary>
public static class VehicleModelCatalog
{
    private sealed record Item(string Brand, string Name, VehicleBodyType BodyType, string AxleConfig, int AxleCount,
        decimal MaxGrossKg, decimal TareKg, decimal FuelLPer100Km);

    private static readonly Item[] Items =
    [
        // Çekiciler: azami ağırlık çekici + yarı römork (5 dingil) kombinasyonu, dara yalnız çekici.
        new("Ford Trucks", "F-MAX 500", Tractor, "4x2", 2, 40_000, 7_900, 31),
        new("Ford Trucks", "1848T", Tractor, "4x2", 2, 40_000, 7_600, 32),
        new("Mercedes-Benz", "Actros 1845 LS", Tractor, "4x2", 2, 40_000, 7_700, 30),
        new("Mercedes-Benz", "Actros 1851 LS", Tractor, "4x2", 2, 40_000, 7_800, 31),
        new("MAN", "TGX 18.470", Tractor, "4x2", 2, 40_000, 7_600, 30),
        new("MAN", "TGX 18.510", Tractor, "4x2", 2, 40_000, 7_700, 31),
        new("Scania", "R 450", Tractor, "4x2", 2, 40_000, 7_600, 30),
        new("Scania", "S 500", Tractor, "4x2", 2, 40_000, 7_900, 31),
        new("Volvo", "FH 500", Tractor, "4x2", 2, 40_000, 7_700, 31),
        new("Volvo", "FH 460", Tractor, "4x2", 2, 40_000, 7_600, 30),
        new("DAF", "XF 480 FT", Tractor, "4x2", 2, 40_000, 7_400, 30),
        new("Iveco", "S-Way 480", Tractor, "4x2", 2, 40_000, 7_500, 31),
        new("Renault Trucks", "T 480", Tractor, "4x2", 2, 40_000, 7_600, 31),
        new("BMC", "Tuğra 1848", Tractor, "4x2", 2, 40_000, 7_800, 32),

        // Damperliler (8x4: 4 dingil 32 t, 6x4: 3 dingil 26 t); dara üstyapı dahil.
        new("Ford Trucks", "4142D", Tipper, "8x4", 4, 32_000, 14_000, 45),
        new("Ford Trucks", "3542D", Tipper, "8x4", 4, 32_000, 13_600, 44),
        new("Mercedes-Benz", "Arocs 4145 K", Tipper, "8x4", 4, 32_000, 14_300, 45),
        new("Mercedes-Benz", "Arocs 3345 K", Tipper, "6x4", 3, 26_000, 11_800, 40),
        new("MAN", "TGS 41.430", Tipper, "8x4", 4, 32_000, 14_000, 45),
        new("MAN", "TGS 33.430", Tipper, "6x4", 3, 26_000, 11_600, 40),
        new("Scania", "G 450 XT", Tipper, "8x4", 4, 32_000, 14_200, 44),
        new("Volvo", "FMX 460", Tipper, "8x4", 4, 32_000, 14_300, 45),
        new("DAF", "CF 450 FAD", Tipper, "8x4", 4, 32_000, 13_900, 44),
        new("Iveco", "X-Way 410", Tipper, "8x4", 4, 32_000, 13_800, 44),
        new("Renault Trucks", "C 430", Tipper, "8x4", 4, 32_000, 14_000, 45),
        new("BMC", "Tuğra 4142", Tipper, "8x4", 4, 32_000, 14_100, 46),

        // Beton mikserleri (8x4); dara tambur dahil, boş.
        new("Ford Trucks", "4142M", Mixer, "8x4", 4, 32_000, 13_500, 48),
        new("Mercedes-Benz", "Arocs 4142 B", Mixer, "8x4", 4, 32_000, 13_800, 48),
        new("MAN", "TGS 41.400", Mixer, "8x4", 4, 32_000, 13_600, 48),
        new("Scania", "P 410 XT", Mixer, "8x4", 4, 32_000, 13_700, 47),
        new("Volvo", "FMX 420", Mixer, "8x4", 4, 32_000, 13_900, 48),
        new("BMC", "Tuğra 4136", Mixer, "8x4", 4, 32_000, 13_700, 49),

        // Kamyonlar (4x2: 2 dingil 18 t, 6x2: 3 dingil 26 t); dara kasa dahil.
        new("Ford Trucks", "1833", Truck, "4x2", 2, 18_000, 8_000, 27),
        new("Ford Trucks", "2533", Truck, "6x2", 3, 26_000, 9_600, 30),
        new("Mercedes-Benz", "Axor 1840", Truck, "4x2", 2, 18_000, 8_100, 28),
        new("Mercedes-Benz", "Atego 1218", Truck, "4x2", 2, 12_000, 5_200, 20),
        new("MAN", "TGL 12.250", Truck, "4x2", 2, 12_000, 5_000, 20),
        new("MAN", "TGM 18.290", Truck, "4x2", 2, 18_000, 7_400, 26),
        new("Isuzu", "NPR 10", Truck, "4x2", 2, 8_500, 3_600, 16),
        new("BMC", "Pro 827", Truck, "4x2", 2, 8_500, 3_900, 17),

        // Tankerler (6x2 / 8x2).
        new("Mercedes-Benz", "Actros 2542 L", Tanker, "6x2", 3, 26_000, 10_500, 32),
        new("Ford Trucks", "3542 Tanker", Tanker, "8x2", 4, 32_000, 12_500, 36),

        // Hafif ticari.
        new("Ford", "Transit 350 L Kamyonet", Van, "4x2", 2, 3_500, 2_100, 10),
        new("Isuzu", "D-Max Kamyonet", Van, "4x4", 2, 3_100, 2_000, 9),
    ];

    /// <summary>Katalogda olmayan kayıtları ekler (marka + model + yapı anahtarıyla).</summary>
    public static async Task SeedAsync(PlatformDbContext db, CancellationToken cancellationToken = default)
    {
        var existing = (await db.VehicleModels.IgnoreQueryFilters().AsNoTracking()
                .Select(m => new { m.Brand, m.Name, m.BodyType })
                .ToListAsync(cancellationToken))
            .Select(m => (m.Brand, m.Name, m.BodyType))
            .ToHashSet();

        var missing = Items.Where(i => !existing.Contains((i.Brand, i.Name, i.BodyType))).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        db.VehicleModels.AddRange(missing.Select(i => new VehicleModel
        {
            Brand = i.Brand,
            Name = i.Name,
            BodyType = i.BodyType,
            AxleConfig = i.AxleConfig,
            AxleCount = i.AxleCount,
            MaxGrossKg = i.MaxGrossKg,
            TypicalTareKg = i.TareKg,
            AvgFuelLPer100Km = i.FuelLPer100Km,
            IsApproved = true,
        }));
        await db.SaveChangesAsync(cancellationToken);
    }
}
