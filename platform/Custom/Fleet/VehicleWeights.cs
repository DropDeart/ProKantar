using BaseForge.Core.Exceptions;

namespace Platform.Custom.Fleet;

/// <summary>Azami yüklü ağırlık için kabul edilen aralık (katalog ve ruhsat değeri ortak).</summary>
internal static class VehicleWeights
{
    public const decimal MinGrossKg = 500m;
    public const decimal MaxGrossKg = 60_000m;

    /// <summary>Değer verilmişse aralıkta olmalı.</summary>
    public static void RequireGrossInRange(decimal? kg, string field, string label)
    {
        if (kg is < MinGrossKg or > MaxGrossKg)
        {
            throw new ValidationException(field, $"{label} 500 ile 60.000 kg arasında olmalı.");
        }
    }
}
