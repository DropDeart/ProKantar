namespace Tracking.Domain;

/// <summary>Telefondan gelen tek konum.</summary>
public sealed record PingSample(
    double Latitude,
    double Longitude,
    double? AccuracyM,
    double? SpeedMps,
    double? Heading,
    DateTimeOffset RecordedAt);
