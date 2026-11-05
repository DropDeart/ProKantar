using System.Text.RegularExpressions;

namespace Kantar.Custom.Validation;

/// <summary>GİB e-İrsaliye posta kodu kuralı: 5 hane, ilk iki hane il plaka kodu (01–81).</summary>
public static partial class PostalCodes
{
    /// <summary>Kullanıcıya gösterilen hata metni.</summary>
    public const string Error = "Posta kodu 5 haneli olmalı ve il koduyla (01–81) başlamalı.";

    /// <summary>Posta kodu kurala uyuyor mu.</summary>
    public static bool IsValid(string? value) => value is { Length: 5 } && Pattern().IsMatch(value);

    [GeneratedRegex("^((0[1-9])|([1-7][0-9])|(8[0-1]))[0-9]{3}$")]
    private static partial Regex Pattern();
}
