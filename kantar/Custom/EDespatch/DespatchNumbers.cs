using System.Globalization;
using Kantar.Data;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch;

/// <summary>
/// e-İrsaliye belge numarası: <c>{önek}{yıl}{9 hane sıra}</c> (ör. PKN2026000000001). Sıra kiracı + önek + yıl
/// başına <c>EDespatchNumberSequences</c> sayacından, advisory lock altında verilir. Sayaç çağıranın
/// <see cref="KantarDbContext"/> transaction'ında artar: irsaliye kaydı geri alınırsa numara da geri döner, boşluk oluşmaz.
/// </summary>
public static class DespatchNumbers
{
    /// <summary>Sıra hanesi.</summary>
    public const int SequenceDigits = 9;

    /// <summary>
    /// Sıradaki numarayı ayırır. Açık bir transaction ister; kilit commit'e kadar tutulur, eş zamanlı gönderim
    /// aynı numarayı alamaz. Yıl Türkiye saatine göre <paramref name="issuedAt"/>'ten alınır.
    /// </summary>
    public static async Task<string> AllocateAsync(KantarDbContext db, Guid tenantId, string prefix, DateTimeOffset issuedAt, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("İrsaliye numarası transaction içinde ayrılmalı.");
        }

        var year = TurkeyTime.ToLocal(issuedAt).Year;
        var lockKey = $"edespatch-no|{tenantId:N}|{prefix}|{year}";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);

        // ToListAsync: INSERT alt sorguya sarılamaz; LINQ operatörü (Single vb.) eklenmemeli.
        var next = await db.Database.SqlQuery<long>($"""
            INSERT INTO "EDespatchNumberSequences" ("TenantId", "Prefix", "Year", "LastNumber")
            VALUES ({tenantId}, {prefix}, {year}, 1)
            ON CONFLICT ("TenantId", "Prefix", "Year") DO UPDATE SET "LastNumber" = "EDespatchNumberSequences"."LastNumber" + 1
            RETURNING "LastNumber" AS "Value"
            """).ToListAsync(ct);
        return Format(prefix, year, next.Single());
    }

    /// <summary>Numarayı biçimler.</summary>
    public static string Format(string prefix, int year, long sequence)
        => string.Create(CultureInfo.InvariantCulture, $"{prefix}{year:0000}{sequence.ToString(new string('0', SequenceDigits), CultureInfo.InvariantCulture)}");
}
