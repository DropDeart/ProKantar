using BaseForge.Core.Exceptions;

namespace Tracking.Features.DriverApp;

/// <summary>Konum paketinin kuralları; ihlalde 400 (ValidationException).</summary>
internal static class PingBatchValidator
{
    /// <summary>Çevrimdışı biriken konumların kabul edildiği en eski geçmiş.</summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);

    /// <summary>Cihaz saati sapmasına tolerans.</summary>
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    public static void Validate(IReadOnlyList<PingInput>? pings, int maxBatchSize, DateTimeOffset now)
    {
        if (pings is null || pings.Count == 0)
        {
            throw new ValidationException("pings", "En az bir konum gönderilmeli.");
        }

        if (pings.Count > maxBatchSize)
        {
            throw new ValidationException("pings", $"Tek istekte en fazla {maxBatchSize} konum gönderilebilir.");
        }

        for (var i = 0; i < pings.Count; i++)
        {
            var error = Check(pings[i], now);
            if (error is not null)
            {
                throw new ValidationException($"pings[{i}]", error);
            }
        }
    }

    private static string? Check(PingInput ping, DateTimeOffset now) => ping switch
    {
        { Latitude: < -90 or > 90 } => "Enlem -90 ile 90 arasında olmalı.",
        { Longitude: < -180 or > 180 } => "Boylam -180 ile 180 arasında olmalı.",
        { AccuracyM: < 0 } => "Doğruluk negatif olamaz.",
        _ when ping.RecordedAt > now + MaxClockSkew => "Konum zamanı gelecekte.",
        _ when ping.RecordedAt < now - MaxAge => $"Konum {MaxAge.TotalDays} günden eski.",
        _ => null,
    };
}
