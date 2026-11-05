using Microsoft.Extensions.Options;
using Tracking.Configuration;
using Tracking.Entities;

namespace Tracking.Domain;

/// <summary>Konuma göre durakların varış/ayrılışını işler.</summary>
public interface IStopProgressTracker
{
    /// <summary>Tek konumu seferin duraklarına uygular.</summary>
    void Apply(Trip trip, PingSample ping);
}

/// <summary>
/// Yarıçap içine girince varış, yarıçap × <see cref="TrackingOptions.DepartureFactor"/> dışına çıkınca ayrılış.
/// Doğruluğu düşük konumlar yok sayılır. Konumlar zaman sırasıyla verilmelidir; durağın varışından eski konum
/// ayrılış saymaz.
/// </summary>
internal sealed class StopProgressTracker(IOptions<TrackingOptions> options) : IStopProgressTracker
{
    private readonly TrackingOptions _options = options.Value;

    public void Apply(Trip trip, PingSample ping)
    {
        ArgumentNullException.ThrowIfNull(trip);
        ArgumentNullException.ThrowIfNull(ping);

        if (ping.AccuracyM > _options.MaxAccuracyM)
        {
            return;
        }

        foreach (var stop in trip.Stops)
        {
            var distance = GeoDistance.Meters(ping.Latitude, ping.Longitude, stop.Location.Y, stop.Location.X);

            if (stop.Status == StopStatus.Pending && distance <= stop.RadiusM)
            {
                stop.MarkArrived(ping.RecordedAt);
            }
            else if (stop.Status == StopStatus.Arrived && distance > stop.RadiusM * _options.DepartureFactor
                     && !(ping.RecordedAt < stop.ArrivedAt))
            {
                stop.MarkDeparted(ping.RecordedAt);
            }
        }
    }
}
