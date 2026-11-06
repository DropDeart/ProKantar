using System.Text.RegularExpressions;

namespace Platform.Custom.Validation;

/// <summary>Türk plakası anahtarı ve biçim kontrolü. Araç plakası bu anahtarla saklanır (bkz. VehicleGuards).</summary>
public static partial class Plates
{
    /// <summary>Boşluksuz büyük harf plaka anahtarı (34ABC123).</summary>
    public static string Key(string? value)
        => new string((value ?? string.Empty).Where(char.IsAsciiLetterOrDigit).ToArray()).ToUpperInvariant();

    /// <summary>İl kodu (01–81) + 1–3 harf + 2–4 rakam.</summary>
    public static bool IsValid(string key) => PlatePattern().IsMatch(key);

    [GeneratedRegex("^(0[1-9]|[1-7][0-9]|8[01])[A-Z]{1,3}[0-9]{2,4}$")]
    private static partial Regex PlatePattern();
}
