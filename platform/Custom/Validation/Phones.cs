namespace Platform.Custom.Validation;

/// <summary>Cep telefonu anahtarı (5XXXXXXXXX) ve maskeleme. Şoför telefonu bu anahtarla saklanır (bkz. DriverGuards).</summary>
public static class Phones
{
    /// <summary>Rakamlara indirger, baştaki 90/0'ı atar: "+90 532 123 45 67" → "5321234567".</summary>
    public static string Key(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("90", StringComparison.Ordinal))
        {
            return digits[2..];
        }

        return digits.Length == 11 && digits[0] == '0' ? digits[1..] : digits;
    }

    /// <summary>Türk cep telefonu anahtarı mı (10 hane, 5 ile başlar).</summary>
    public static bool IsValidMobile(string key) => key.Length == 10 && key[0] == '5' && key.All(char.IsAsciiDigit);

    /// <summary>"5321234567" → "532*****67": tanıma yeter, numara sızdırmaz.</summary>
    public static string Mask(string phone)
        => phone.Length < 6 ? "***" : $"{phone[..3]}{new string('*', phone.Length - 5)}{phone[^2..]}";
}
