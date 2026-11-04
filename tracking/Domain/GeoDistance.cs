namespace Tracking.Domain;

/// <summary>İki WGS84 nokta arası mesafe (haversine).</summary>
public static class GeoDistance
{
    private const double EarthRadiusM = 6_371_008.8;

    /// <summary>Metre cinsinden mesafe.</summary>
    public static double Meters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2)
              + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * EarthRadiusM * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
