namespace Tracking.Configuration;

/// <summary>Takip kuralları (appsettings "Tracking").</summary>
public sealed class TrackingOptions
{
    /// <summary>Yapılandırma bölümü adı.</summary>
    public const string Section = "Tracking";

    /// <summary>Ham konumların saklanma süresi (ay).</summary>
    public int RetentionMonths { get; set; } = 6;

    /// <summary>Tek istekte kabul edilen en fazla konum.</summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>Bundan kötü doğruluktaki konum durak kararına katılmaz (m).</summary>
    public double MaxAccuracyM { get; set; } = 100;

    /// <summary>Ayrılış için yarıçap çarpanı; sınırda titremeyi önler.</summary>
    public double DepartureFactor { get; set; } = 1.25;
}
