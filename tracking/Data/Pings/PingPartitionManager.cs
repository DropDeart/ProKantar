using System.Globalization;
using BaseForge.Core.Interfaces;

namespace Tracking.Data.Pings;

/// <summary>location_pings tablosunu ve aylık bölümlerini yönetir.</summary>
public interface IPingPartitionManager
{
    /// <summary>Tablo yoksa oluşturur (idempotent).</summary>
    Task EnsureTableAsync(CancellationToken cancellationToken);

    /// <summary>Geçen aydan itibaren <paramref name="monthsAhead"/> ay ilerisine kadar bölümleri açar.</summary>
    Task EnsurePartitionsAsync(DateTimeOffset now, int monthsAhead, CancellationToken cancellationToken);

    /// <summary>Saklama süresi dolan bölümleri siler; silinen adları döner.</summary>
    Task<IReadOnlyList<string>> DropExpiredAsync(DateTimeOffset now, int retentionMonths, CancellationToken cancellationToken);
}

/// <summary>
/// Ham konumlar EF modelinde değil: aylık bölümlü tablo (RANGE recorded_at) eski ayı tek
/// DROP ile siler, 6 aylık saklama böylece ucuz kalır.
/// </summary>
internal sealed class PingPartitionManager(ISqlQuery sql) : IPingPartitionManager
{
    private const string CreateTableSql = """
        CREATE EXTENSION IF NOT EXISTS postgis;
        CREATE TABLE IF NOT EXISTS location_pings (
            id             bigserial,
            trip_id        uuid        NOT NULL,
            driver_user_id uuid        NOT NULL,
            location       geometry(Point, 4326) NOT NULL,
            accuracy_m     real,
            speed_mps      real,
            heading        real,
            recorded_at    timestamptz NOT NULL,
            received_at    timestamptz NOT NULL DEFAULT now(),
            PRIMARY KEY (id, recorded_at)
        ) PARTITION BY RANGE (recorded_at);
        CREATE INDEX IF NOT EXISTS ix_location_pings_trip_time ON location_pings (trip_id, recorded_at);
        """;

    private const string ListPartitionsSql = """
        SELECT child.relname
        FROM pg_inherits i
        JOIN pg_class child  ON child.oid  = i.inhrelid
        JOIN pg_class parent ON parent.oid = i.inhparent
        WHERE parent.relname = 'location_pings'
        """;

    public Task EnsureTableAsync(CancellationToken cancellationToken)
        => sql.ExecuteAsync(CreateTableSql, cancellationToken: cancellationToken);

    public async Task EnsurePartitionsAsync(DateTimeOffset now, int monthsAhead, CancellationToken cancellationToken)
    {
        var first = PingPartitions.MonthStart(now).AddMonths(-1);
        for (var month = first; month <= first.AddMonths(monthsAhead + 1); month = month.AddMonths(1))
        {
            // Bölüm adı ve sınırlar koddan üretilir (kullanıcı girdisi değil); DDL parametre almaz.
            var ddl = string.Format(
                CultureInfo.InvariantCulture,
                "CREATE TABLE IF NOT EXISTS {0} PARTITION OF location_pings FOR VALUES FROM ('{1:yyyy-MM-dd}') TO ('{2:yyyy-MM-dd}');",
                PingPartitions.NameFor(month), month, month.AddMonths(1));
            await sql.ExecuteAsync(ddl, cancellationToken: cancellationToken);
        }
    }

    public async Task<IReadOnlyList<string>> DropExpiredAsync(DateTimeOffset now, int retentionMonths, CancellationToken cancellationToken)
    {
        var cutoff = PingPartitions.MonthStart(now).AddMonths(-retentionMonths);
        var partitions = await sql.QueryAsync<string>(ListPartitionsSql, cancellationToken: cancellationToken);

        var expired = partitions
            .Where(name => PingPartitions.TryParseMonth(name, out var month) && month < cutoff)
            .ToList();

        foreach (var name in expired)
        {
            await sql.ExecuteAsync($"DROP TABLE IF EXISTS {name};", cancellationToken: cancellationToken);
        }

        return expired;
    }
}
