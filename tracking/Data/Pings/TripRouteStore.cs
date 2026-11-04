using BaseForge.Core.Interfaces;

namespace Tracking.Data.Pings;

/// <summary>Seferin kalıcı rotası (Trips.Route).</summary>
public interface ITripRouteStore
{
    /// <summary>Konumlardan rotayı ve uzunluğunu seferin üzerine yazar.</summary>
    Task RecordAsync(Guid tripId, DateTimeOffset from, CancellationToken cancellationToken);

    /// <summary>Kayıtlı rota (GeoJSON); yoksa null.</summary>
    Task<string?> GetGeoJsonAsync(Guid tripId, CancellationToken cancellationToken);
}

/// <summary>PostGIS ile <see cref="ITripRouteStore"/>. Ham konumlar silinse de rota kalır.</summary>
internal sealed class TripRouteStore(ISqlQuery sql) : ITripRouteStore
{
    private const string RecordSql = """
        UPDATE "Trips" AS t
        SET "Route" = r.line, "DistanceM" = ST_Length(r.line::geography)
        FROM (
            SELECT ST_MakeLine(location ORDER BY recorded_at) AS line
            FROM location_pings
            WHERE trip_id = @TripId AND recorded_at >= @From
            HAVING count(*) >= 2
        ) AS r
        WHERE t."Id" = @TripId
        """;

    private const string GetSql = """SELECT ST_AsGeoJSON("Route") FROM "Trips" WHERE "Id" = @TripId AND "Route" IS NOT NULL""";

    public Task RecordAsync(Guid tripId, DateTimeOffset from, CancellationToken cancellationToken)
        => sql.ExecuteAsync(RecordSql, new { TripId = tripId, From = from.ToUniversalTime() }, cancellationToken);

    public Task<string?> GetGeoJsonAsync(Guid tripId, CancellationToken cancellationToken)
        => sql.QuerySingleOrDefaultAsync<string?>(GetSql, new { TripId = tripId }, cancellationToken);
}
