using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Tracking.Domain;

/// <summary>WGS84 (SRID 4326) nokta üretimi.</summary>
public static class GeoPoints
{
    private static readonly GeometryFactory Factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    /// <summary>Enlem/boylamdan nokta (X = boylam, Y = enlem).</summary>
    public static Point Create(double latitude, double longitude)
        => Factory.CreatePoint(new Coordinate(longitude, latitude));
}
