using BaseForge.Core.Interfaces;
using Tracking.Domain;

namespace Tracking.Data.Pings;

/// <summary>Ham konum yazma/okuma.</summary>
public interface ILocationPingStore
{
    /// <summary>Konumları tek sorguda yazar.</summary>
    Task AddAsync(Guid tripId, Guid driverUserId, IReadOnlyList<PingSample> pings, CancellationToken cancellationToken);

    /// <summary>Seferin konumlarından çizgi (GeoJSON LineString); en az iki konum yoksa null.</summary>
    Task<string?> GetLineGeoJsonAsync(Guid tripId, DateTimeOffset from, CancellationToken cancellationToken);

    /// <summary>Seferin <paramref name="from"/> sonrası en yeni konum zamanı; konum yoksa null.</summary>
    Task<DateTimeOffset?> GetLatestRecordedAtAsync(Guid tripId, DateTimeOffset from, CancellationToken cancellationToken);

    /// <summary>Seferin son konumu.</summary>
    Task<LastLocation?> GetLastAsync(Guid tripId, DateTimeOffset from, CancellationToken cancellationToken);
}

/// <summary>Seferin bilinen son konumu.</summary>
public sealed record LastLocation(double Latitude, double Longitude, double? SpeedMps, double? Heading, DateTimeOffset RecordedAt);

/// <summary>Dapper + PostGIS ile <see cref="ILocationPingStore"/>.</summary>
internal sealed class LocationPingStore(ISqlQuery sql) : ILocationPingStore
{
    // Diziler unnest ile satıra açılır: 100 konum tek INSERT.
    private const string InsertSql = """
        INSERT INTO location_pings (trip_id, driver_user_id, location, accuracy_m, speed_mps, heading, recorded_at)
        SELECT @TripId, @DriverUserId, ST_SetSRID(ST_MakePoint(p.lon, p.lat), 4326), p.acc, p.spd, p.hdg, p.rec
        FROM unnest(@Lats, @Lons, @Accuracies, @Speeds, @Headings, @RecordedAts) AS p(lat, lon, acc, spd, hdg, rec)
        """;

    // recorded_at alt sınırı bölüm budamasını (partition pruning) sağlar.
    private const string LineSql = """
        SELECT ST_AsGeoJSON(ST_MakeLine(location ORDER BY recorded_at))
        FROM location_pings
        WHERE trip_id = @TripId AND recorded_at >= @From
        HAVING count(*) >= 2
        """;

    private const string LastSql = """
        SELECT ST_Y(location) AS "Latitude", ST_X(location) AS "Longitude",
               speed_mps::float8 AS "SpeedMps", heading::float8 AS "Heading", recorded_at AS "RecordedAt"
        FROM location_pings
        WHERE trip_id = @TripId AND recorded_at >= @From
        ORDER BY recorded_at DESC
        LIMIT 1
        """;

    private const string LatestSql = """
        SELECT max(recorded_at) FROM location_pings
        WHERE trip_id = @TripId AND recorded_at >= @From
        """;

    public Task AddAsync(Guid tripId, Guid driverUserId, IReadOnlyList<PingSample> pings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pings);
        return sql.ExecuteAsync(InsertSql, new
        {
            TripId = tripId,
            DriverUserId = driverUserId,
            Lats = pings.Select(p => p.Latitude).ToArray(),
            Lons = pings.Select(p => p.Longitude).ToArray(),
            Accuracies = pings.Select(p => (float?)p.AccuracyM).ToArray(),
            Speeds = pings.Select(p => (float?)p.SpeedMps).ToArray(),
            Headings = pings.Select(p => (float?)p.Heading).ToArray(),
            RecordedAts = pings.Select(p => p.RecordedAt.ToUniversalTime()).ToArray(),
        }, cancellationToken);
    }

    public Task<string?> GetLineGeoJsonAsync(Guid tripId, DateTimeOffset from, CancellationToken cancellationToken)
        => sql.QuerySingleOrDefaultAsync<string?>(LineSql, new { TripId = tripId, From = from.ToUniversalTime() }, cancellationToken);

    public async Task<DateTimeOffset?> GetLatestRecordedAtAsync(Guid tripId, DateTimeOffset from, CancellationToken cancellationToken)
    {
        var latest = await sql.QuerySingleOrDefaultAsync<DateTime?>(LatestSql, new { TripId = tripId, From = from.ToUniversalTime() }, cancellationToken);
        return latest is { } value ? new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)) : null;
    }

    public async Task<LastLocation?> GetLastAsync(Guid tripId, DateTimeOffset from, CancellationToken cancellationToken)
    {
        var row = await sql.QuerySingleOrDefaultAsync<LastPingRow>(LastSql, new { TripId = tripId, From = from.ToUniversalTime() }, cancellationToken);
        return row is null
            ? null
            : new LastLocation(row.Latitude, row.Longitude, row.SpeedMps, row.Heading,
                new DateTimeOffset(DateTime.SpecifyKind(row.RecordedAt, DateTimeKind.Utc)));
    }

    /// <summary>Dapper satırı (Npgsql timestamptz'yi UTC DateTime okur).</summary>
    private sealed class LastPingRow
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public double? SpeedMps { get; init; }
        public double? Heading { get; init; }
        public DateTime RecordedAt { get; init; }
    }
}
