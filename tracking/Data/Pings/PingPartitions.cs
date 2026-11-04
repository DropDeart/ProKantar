using System.Globalization;

namespace Tracking.Data.Pings;

/// <summary>location_pings tablosunun aylık bölüm (partition) adları.</summary>
internal static class PingPartitions
{
    private const string Prefix = "location_pings_";
    private const string MonthFormat = "yyyyMM";

    /// <summary>Ayın ilk günü (UTC).</summary>
    public static DateTimeOffset MonthStart(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero);
    }

    /// <summary>Aya ait bölüm adı (örn. location_pings_202611).</summary>
    public static string NameFor(DateTimeOffset monthStart)
        => Prefix + monthStart.ToString(MonthFormat, CultureInfo.InvariantCulture);

    /// <summary>Bölüm adından ayı çözer.</summary>
    public static bool TryParseMonth(string partitionName, out DateTimeOffset monthStart)
    {
        monthStart = default;
        if (!partitionName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return DateTimeOffset.TryParseExact(
            partitionName[Prefix.Length..], MonthFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out monthStart);
    }
}
