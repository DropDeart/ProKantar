namespace Kantar.Custom;

/// <summary>Türkiye saati (Europe/Istanbul); gün sınırı gerektiren numaralama/raporlar bunu kullanır.</summary>
internal static class TurkeyTime
{
    private const string ZoneId = "Europe/Istanbul";

    private static readonly TimeZoneInfo Zone = Resolve();

    /// <summary>Verilen anı Türkiye saatine çevirir.</summary>
    public static DateTimeOffset ToLocal(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Zone);

    /// <summary>
    /// Sistem saat dilimi veritabanı yoksa (tzdata'sız imaj) 2016'dan beri sabit olan UTC+3'e düşer.
    /// </summary>
    private static TimeZoneInfo Resolve()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(ZoneId, TimeSpan.FromHours(3), ZoneId, ZoneId);
        }
    }
}
