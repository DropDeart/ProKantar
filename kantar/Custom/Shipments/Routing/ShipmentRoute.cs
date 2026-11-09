using System.Globalization;
using System.Text.Json;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Kantar.Custom.Shipments.Routing;

/// <summary>Yol hesabı ayarı (<c>Routing:OsrmUrl</c>). Varsayılan OSRM demo sunucusu yalnız geliştirme içindir.</summary>
public sealed class RoutingOptions
{
    /// <summary>Ayar bölümü.</summary>
    public const string Section = "Routing";

    /// <summary>OSRM sunucusu (sonda / ile).</summary>
    public string OsrmUrl { get; set; } = "https://router.project-osrm.org/";
}

/// <summary>İki durak arası yol.</summary>
/// <param name="FromSequence">Çıkış durağı sırası.</param>
/// <param name="ToSequence">Varış durağı sırası.</param>
/// <param name="DistanceM">Mesafe (m).</param>
/// <param name="DurationS">Süre (sn, trafiksiz tahmin).</param>
public sealed record RouteLeg(int FromSequence, int ToSequence, double DistanceM, double DurationS);

/// <summary>Sevkiyatın yol ön izlemesi.</summary>
/// <param name="DistanceM">Toplam mesafe (m).</param>
/// <param name="DurationS">Toplam sürüş süresi (sn).</param>
/// <param name="Legs">Durak arası bacaklar.</param>
/// <param name="GeoJson">Yol çizgisi (GeoJSON LineString); yaklaşıkta düz çizgi.</param>
/// <param name="Approximate">Yol servisine ulaşılamadı; mesafe kuş uçuşu × 1,3, süre 60 km/sa varsayımı.</param>
public sealed record ShipmentRoutePreview(double DistanceM, double DurationS, IReadOnlyList<RouteLeg> Legs, string? GeoJson, bool Approximate);

/// <summary>Durak koordinatlarından yol (OSRM). Sonuç bir gün önbellekte tutulur (aynı güzergah tekrar sorulmaz).</summary>
public sealed partial class OsrmRoutePlanner(HttpClient http, IMemoryCache cache, ILogger<OsrmRoutePlanner> logger)
{
    /// <summary>HttpClient adı.</summary>
    public const string HttpClientName = "osrm";

    private static readonly TimeSpan CacheFor = TimeSpan.FromDays(1);

    /// <summary>Sıralı noktalar arası yol; servis hata verirse yaklaşık hesap.</summary>
    public async Task<ShipmentRoutePreview> PlanAsync(IReadOnlyList<(int Sequence, double Lat, double Lng)> points, CancellationToken ct)
    {
        if (points.Count < 2)
        {
            return new ShipmentRoutePreview(0, 0, [], null, false);
        }

        var coords = string.Join(';', points.Select(p => string.Create(CultureInfo.InvariantCulture, $"{p.Lng:0.######},{p.Lat:0.######}")));
        if (cache.TryGetValue(coords, out ShipmentRoutePreview? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            using var response = await http.GetAsync($"route/v1/driving/{coords}?overview=full&geometries=geojson&steps=false", ct);
            response.EnsureSuccessStatusCode();
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = json.RootElement;
            if (root.GetProperty("code").GetString() != "Ok")
            {
                throw new InvalidOperationException(root.GetProperty("code").GetString());
            }

            var route = root.GetProperty("routes")[0];
            var legs = route.GetProperty("legs").EnumerateArray()
                .Select((leg, i) => new RouteLeg(points[i].Sequence, points[i + 1].Sequence,
                    leg.GetProperty("distance").GetDouble(), leg.GetProperty("duration").GetDouble()))
                .ToList();
            var preview = new ShipmentRoutePreview(route.GetProperty("distance").GetDouble(), route.GetProperty("duration").GetDouble(),
                legs, route.GetProperty("geometry").GetRawText(), false);
            cache.Set(coords, preview, CacheFor);
            return preview;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException or KeyNotFoundException)
        {
            LogFallback(logger, ex.Message);
            return Approximate(points);
        }
    }

    /// <summary>Kuş uçuşu × 1,3 (karayolu dolanma katsayısı), 60 km/sa.</summary>
    private static ShipmentRoutePreview Approximate(IReadOnlyList<(int Sequence, double Lat, double Lng)> points)
    {
        var legs = points.Zip(points.Skip(1), (a, b) =>
        {
            var meters = Haversine(a.Lat, a.Lng, b.Lat, b.Lng) * 1.3;
            return new RouteLeg(a.Sequence, b.Sequence, meters, meters / (60_000d / 3600d));
        }).ToList();
        var line = JsonSerializer.Serialize(new { type = "LineString", coordinates = points.Select(p => new[] { p.Lng, p.Lat }) });
        return new ShipmentRoutePreview(legs.Sum(l => l.DistanceM), legs.Sum(l => l.DurationS), legs, line, true);
    }

    private static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6_371_000;
        double Rad(double d) => d * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLng = Rad(lng2 - lng1);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * R * Math.Asin(Math.Sqrt(h));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Yol servisi yanıt vermedi ({Reason}); yaklaşık mesafe kullanıldı.")]
    private static partial void LogFallback(ILogger logger, string reason);
}

/// <summary>Sevkiyatın yol ön izlemesi (durak sırasıyla): yol çizgisi, durak arası mesafe ve süre. Taslakta da çalışır.</summary>
public sealed class ShipmentRouteQuery : IQuery<ShipmentRoutePreview>
{
    /// <summary>Sevkiyat.</summary>
    public Guid ShipmentId { get; set; }
}

internal sealed class ShipmentRouteHandler(KantarDbContext db, OsrmRoutePlanner planner) : IQueryHandler<ShipmentRouteQuery, ShipmentRoutePreview>
{
    public async Task<ShipmentRoutePreview> Handle(ShipmentRouteQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!await db.Shipments.AnyAsync(s => s.Id == request.ShipmentId, cancellationToken))
        {
            throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        }

        var stops = await db.ShipmentStops.AsNoTracking()
            .Where(s => s.ShipmentId == request.ShipmentId)
            .OrderBy(s => s.Sequence)
            .Select(s => new { s.Sequence, s.Latitude, s.Longitude })
            .ToListAsync(cancellationToken);
        return await planner.PlanAsync(stops.Select(s => (s.Sequence, (double)s.Latitude, (double)s.Longitude)).ToList(), cancellationToken);
    }
}
