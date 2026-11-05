using System.Globalization;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Data;
using Kantar.Entities;
using Kantar.Features.Shipments;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kantar.Custom.Shipments;

/// <summary>
/// Sevkiyat numarası: <c>{istasyon kodu}-S{yyMMdd}-{sıra}</c>. Gün Türkiye saatine göre alınır; sıra kiracı+önek
/// başına kilit altında "en büyük + 1" olarak verilir. (Kiracı, numara) veritabanında benzersizdir
/// (<see cref="UniqueIndexSql"/>).
/// </summary>
internal static class ShipmentNumbers
{
    /// <summary>Hata alanı.</summary>
    public const string Field = nameof(Shipment.ShipmentNo);

    /// <summary>
    /// Kiracı içinde benzersiz numara. Silinmiş (soft delete) kayıtlar da dahildir: numaraları yeniden verilmez.
    /// EnsureCreated var olan tabloya indeks eklemediği için açılışta idempotent olarak çalıştırılır.
    /// </summary>
    public const string UniqueIndexSql = """
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_Shipments_TenantId_ShipmentNo"
        ON "Shipments" ("TenantId", "ShipmentNo") WHERE "ShipmentNo" <> ''
        """;

    /// <summary>İstasyon ve planlanan güne (Türkiye saati) göre numara öneki.</summary>
    public static string PrefixFor(string stationCode, DateTimeOffset plannedAt)
        => string.Create(CultureInfo.InvariantCulture, $"{stationCode}-S{TurkeyTime.ToLocal(plannedAt):yyMMdd}-");

    /// <summary>
    /// Öneki için sıradaki numarayı ayırır. Açık bir transaction ister: kiracı+önek kilidi (advisory lock)
    /// commit'e kadar tutulur, eş zamanlı oluşturma aynı numarayı alamaz. Silinen kayıtlar da sayıldığından
    /// numara geri dönmez.
    /// </summary>
    public static async Task<string> AllocateAsync(KantarDbContext db, Guid tenantId, string prefix, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Sevkiyat numarası transaction içinde ayrılmalı.");
        }

        var lockKey = $"shipment-no|{tenantId:N}|{prefix}";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);

        var taken = await db.Shipments.IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && s.ShipmentNo.StartsWith(prefix))
            .Select(s => s.ShipmentNo)
            .ToListAsync(ct);
        var last = taken
            .Select(no => int.TryParse(no.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return string.Create(CultureInfo.InvariantCulture, $"{prefix}{last + 1:000}");
    }

    /// <summary>Numara kiracıda (silinmişler dahil) başka bir sevkiyatta kullanılıyor mu?</summary>
    public static Task<bool> IsTakenAsync(KantarDbContext db, Guid tenantId, string shipmentNo, Guid? selfId, CancellationToken ct)
        => db.Shipments.IgnoreQueryFilters()
            .AnyAsync(s => s.TenantId == tenantId && s.ShipmentNo == shipmentNo && s.Id != selfId, ct);

    /// <summary>Elle verilen numara boş değilse kiracıda benzersiz olmalı.</summary>
    public static async Task RequireUniqueAsync(KantarDbContext db, Guid tenantId, string? shipmentNo, Guid? selfId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(shipmentNo) && await IsTakenAsync(db, tenantId, shipmentNo.Trim(), selfId, ct))
        {
            throw new ValidationException(Field, $"{shipmentNo.Trim()} numaralı sevkiyat zaten var.");
        }
    }

    /// <summary>Kayıt sırasında benzersizlik indeksine takıldı mı (eş zamanlı aynı elle numara)?</summary>
    public static bool IsDuplicate(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UX_Shipments_TenantId_ShipmentNo" };
}

/// <summary>Genel CRUD ucuyla açılan sevkiyatın numarası kiracıda benzersiz olmalı.</summary>
internal sealed class CreateShipmentNumberGuard(KantarDbContext db, ICurrentTenant currentTenant) : IRequestPreProcessor<CreateShipmentCommand>
{
    public Task Process(CreateShipmentCommand request, CancellationToken cancellationToken)
        => ShipmentNumbers.RequireUniqueAsync(db, currentTenant.RequireTenantId(), request.ShipmentNo, null, cancellationToken);
}

/// <summary>Genel CRUD ucuyla değişen numara kiracıda benzersiz olmalı.</summary>
internal sealed class UpdateShipmentNumberGuard(KantarDbContext db, ICurrentTenant currentTenant) : IRequestPreProcessor<UpdateShipmentCommand>
{
    public Task Process(UpdateShipmentCommand request, CancellationToken cancellationToken)
        => ShipmentNumbers.RequireUniqueAsync(db, currentTenant.RequireTenantId(), request.ShipmentNo, request.Id, cancellationToken);
}
