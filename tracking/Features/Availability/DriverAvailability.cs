using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Tracking.Common;

namespace Tracking.Features.Availability;

/// <summary>
/// "Müsaitim / iş arıyorum" modu: şoför açtığında yalnız son konumu tutulur (tek satır), kapatınca ya da
/// <see cref="DriverAvailabilityStore.Ttl"/> boyunca konum gelmezse silinir/görünmez. Firma tam konumu değil yaklaşık mesafeyi görür.
/// </summary>
public interface IDriverAvailabilityStore
{
    /// <summary>Tablo yoksa oluşturur (idempotent).</summary>
    Task EnsureTableAsync(CancellationToken cancellationToken);

    /// <summary>Şoförün son konumunu yazar (müsait).</summary>
    Task UpsertAsync(Guid driverUserId, double latitude, double longitude, double? accuracyM, CancellationToken cancellationToken);

    /// <summary>Müsait modundan çıkar (konum silinir).</summary>
    Task RemoveAsync(Guid driverUserId, CancellationToken cancellationToken);

    /// <summary>Noktaya en yakın müsait şoförler (süresi geçenler hariç).</summary>
    Task<IReadOnlyList<NearbyDriver>> NearbyAsync(double latitude, double longitude, double radiusKm, int limit, CancellationToken cancellationToken);
}

/// <summary>Yakındaki müsait şoför: kullanıcı kimliği ve yuvarlanmış mesafe (tam konum verilmez).</summary>
public sealed record NearbyDriver(Guid DriverUserId, double DistanceKm, DateTimeOffset UpdatedAt);

/// <summary>Dapper + PostGIS ile <see cref="IDriverAvailabilityStore"/>.</summary>
internal sealed class DriverAvailabilityStore(ISqlQuery sql) : IDriverAvailabilityStore
{
    /// <summary>Bu süre konum gelmezse şoför müsait sayılmaz ve satırı silinir.</summary>
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    private const string CreateTableSql = """
        CREATE EXTENSION IF NOT EXISTS postgis;
        CREATE TABLE IF NOT EXISTS driver_availability (
            driver_user_id uuid PRIMARY KEY,
            location       geography(Point, 4326) NOT NULL,
            accuracy_m     real,
            updated_at     timestamptz NOT NULL DEFAULT now()
        );
        CREATE INDEX IF NOT EXISTS ix_driver_availability_location ON driver_availability USING gist (location);
        CREATE INDEX IF NOT EXISTS ix_driver_availability_updated ON driver_availability (updated_at);
        """;

    // Süresi geçen satırlar her yazımda temizlenir (indeksli, ucuz); ayrı zamanlayıcı gerekmez.
    private const string UpsertSql = """
        DELETE FROM driver_availability WHERE updated_at < now() - @Ttl;
        INSERT INTO driver_availability (driver_user_id, location, accuracy_m, updated_at)
        VALUES (@DriverUserId, ST_SetSRID(ST_MakePoint(@Longitude, @Latitude), 4326)::geography, @AccuracyM, now())
        ON CONFLICT (driver_user_id) DO UPDATE
            SET location = EXCLUDED.location, accuracy_m = EXCLUDED.accuracy_m, updated_at = EXCLUDED.updated_at;
        """;

    // Mesafe 0,5 km'ye yuvarlanır: şoförün tam yeri çıkarılamaz.
    private const string NearbySql = """
        SELECT driver_user_id AS "DriverUserId",
               (round((ST_Distance(location, p.pt) / 500.0)::numeric) * 0.5)::float8 AS "DistanceKm",
               updated_at AS "UpdatedAt"
        FROM driver_availability,
             (SELECT ST_SetSRID(ST_MakePoint(@Longitude, @Latitude), 4326)::geography AS pt) p
        WHERE updated_at >= now() - @Ttl AND ST_DWithin(location, p.pt, @RadiusM)
        ORDER BY ST_Distance(location, p.pt)
        LIMIT @Limit
        """;

    public Task EnsureTableAsync(CancellationToken cancellationToken)
        => sql.ExecuteAsync(CreateTableSql, cancellationToken: cancellationToken);

    public Task UpsertAsync(Guid driverUserId, double latitude, double longitude, double? accuracyM, CancellationToken cancellationToken)
        => sql.ExecuteAsync(UpsertSql, new { DriverUserId = driverUserId, Latitude = latitude, Longitude = longitude, AccuracyM = (float?)accuracyM, Ttl }, cancellationToken);

    public Task RemoveAsync(Guid driverUserId, CancellationToken cancellationToken)
        => sql.ExecuteAsync("DELETE FROM driver_availability WHERE driver_user_id = @DriverUserId", new { DriverUserId = driverUserId }, cancellationToken);

    public async Task<IReadOnlyList<NearbyDriver>> NearbyAsync(double latitude, double longitude, double radiusKm, int limit, CancellationToken cancellationToken)
        => (await sql.QueryAsync<NearbyRow>(
                NearbySql, new { Latitude = latitude, Longitude = longitude, RadiusM = radiusKm * 1000, Limit = limit, Ttl }, cancellationToken))
            .Select(r => new NearbyDriver(r.DriverUserId, r.DistanceKm, new DateTimeOffset(DateTime.SpecifyKind(r.UpdatedAt, DateTimeKind.Utc))))
            .ToList();

    /// <summary>Npgsql timestamptz'yi UTC DateTime olarak döndürür; Dapper satırı birebir tiplerle eşler.</summary>
    private sealed record NearbyRow(Guid DriverUserId, double DistanceKm, DateTime UpdatedAt);
}

/// <summary>Şoför müsait modunu açar/konumunu yeniler (uygulama mod açıkken birkaç dakikada bir gönderir).</summary>
public sealed class SetAvailableCommand : ICommand
{
    /// <summary>Enlem.</summary>
    public double Latitude { get; set; }

    /// <summary>Boylam.</summary>
    public double Longitude { get; set; }

    /// <summary>Konum doğruluğu (m).</summary>
    public double? AccuracyM { get; set; }
}

/// <summary>Şoför müsait modunu kapatır (konum silinir).</summary>
public sealed class SetUnavailableCommand : ICommand;

/// <summary>Firma: noktaya (istasyon) yakın müsait şoförler.</summary>
public sealed class NearbyDriversQuery : IQuery<IReadOnlyList<NearbyDriver>>
{
    /// <summary>Merkez enlem.</summary>
    public double Latitude { get; set; }

    /// <summary>Merkez boylam.</summary>
    public double Longitude { get; set; }

    /// <summary>Yarıçap (km, 1–200).</summary>
    public double RadiusKm { get; set; } = 25;
}

internal static class Coordinates
{
    public static void Require(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new ValidationException("Latitude", "Konum geçersiz.");
        }
    }
}

internal sealed class SetAvailableHandler(IDriverAvailabilityStore store, ICurrentUser currentUser) : ICommandHandler<SetAvailableCommand>
{
    public Task Handle(SetAvailableCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Coordinates.Require(request.Latitude, request.Longitude);
        return store.UpsertAsync(currentUser.RequireUserId(), request.Latitude, request.Longitude, request.AccuracyM, cancellationToken);
    }
}

internal sealed class SetUnavailableHandler(IDriverAvailabilityStore store, ICurrentUser currentUser) : ICommandHandler<SetUnavailableCommand>
{
    public Task Handle(SetUnavailableCommand request, CancellationToken cancellationToken)
        => store.RemoveAsync(currentUser.RequireUserId(), cancellationToken);
}

internal sealed class NearbyDriversHandler(IDriverAvailabilityStore store) : IQueryHandler<NearbyDriversQuery, IReadOnlyList<NearbyDriver>>
{
    private const int MaxResults = 50;

    public Task<IReadOnlyList<NearbyDriver>> Handle(NearbyDriversQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Coordinates.Require(request.Latitude, request.Longitude);
        if (request.RadiusKm is < 1 or > 200)
        {
            throw new ValidationException(nameof(request.RadiusKm), "Yarıçap 1 ile 200 km arasında olmalı.");
        }

        return store.NearbyAsync(request.Latitude, request.Longitude, request.RadiusKm, MaxResults, cancellationToken);
    }
}
