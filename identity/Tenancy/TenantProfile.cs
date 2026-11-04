using System.Net.Mail;
using Identity.ErrorHandling;

namespace Identity.Tenancy;

/// <summary>Doğrulanmış firma bilgileri (başvuru ve güncellemede ortak).</summary>
public sealed record TenantProfile(
    string TaxNo,
    string TaxOffice,
    string? MersisNo,
    string? TradeRegistryNo,
    string Phone,
    string Email,
    string? KepAddress,
    string City,
    string District,
    string Address)
{
    /// <summary>İstekten doğrular; ilk hatada 400.</summary>
    public static TenantProfile From(ITenantProfileInput input) => new(
        TaxNumber(input.TaxNo),
        Required(input.TaxOffice, "Vergi dairesi", 100),
        Digits(input.MersisNo, 16, "MERSİS no 16 haneli olmalı."),
        Optional(input.TradeRegistryNo, 30),
        PhoneNumber(input.Phone),
        Mail(input.Email, "Firma e-postası", required: true)!,
        Mail(input.KepAddress, "KEP adresi", required: false),
        Required(input.City, "İl", 60),
        Required(input.District, "İlçe", 60),
        Required(input.Address, "Adres", 500));

    private static string Required(string? value, string label, int max)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            throw IdentityException.Invalid($"{label} zorunlu.");
        }

        return text.Length <= max ? text : throw IdentityException.Invalid($"{label} en fazla {max} karakter olabilir.");
    }

    private static string? Optional(string? value, int max)
    {
        var text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text.Length <= max ? text : throw IdentityException.Invalid($"En fazla {max} karakter olabilir.");
    }

    private static string? Digits(string? value, int length, string error)
    {
        var digits = OnlyDigits(value);
        return digits.Length == 0 ? null : digits.Length == length ? digits : throw IdentityException.Invalid(error);
    }

    private static string PhoneNumber(string? value)
    {
        var digits = OnlyDigits(value);
        if (digits.Length == 12 && digits.StartsWith("90", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }
        else if (digits.Length == 11 && digits[0] == '0')
        {
            digits = digits[1..];
        }

        return digits.Length == 10 ? digits : throw IdentityException.Invalid("Firma telefonu 10 haneli olmalı.");
    }

    private static string? Mail(string? value, string label, bool required)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return required ? throw IdentityException.Invalid($"{label} zorunlu.") : null;
        }

        return text.Length <= 256 && MailAddress.TryCreate(text, out _) ? text : throw IdentityException.Invalid($"{label} geçersiz.");
    }

    /// <summary>VKN (10 hane) ya da TCKN (11 hane), kontrol haneleriyle.</summary>
    private static string TaxNumber(string? value)
    {
        var digits = OnlyDigits(value);
        var valid = digits.Length switch
        {
            10 => IsValidVkn(digits),
            11 => IsValidTckn(digits),
            _ => false,
        };
        return valid ? digits : throw IdentityException.Invalid("Geçerli bir vergi kimlik no (10 hane) ya da TC kimlik no (11 hane) girin.");
    }

    private static bool IsValidVkn(string vkn)
    {
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var t = (vkn[i] - '0' + 9 - i) % 10;
            var p = t * (1 << (9 - i)) % 9;
            sum += t != 0 && p == 0 ? 9 : p;
        }

        return (10 - sum % 10) % 10 == vkn[9] - '0';
    }

    private static bool IsValidTckn(string tckn)
    {
        var d = tckn.Select(c => c - '0').ToArray();
        if (d[0] == 0)
        {
            return false;
        }

        var odd = d[0] + d[2] + d[4] + d[6] + d[8];
        var even = d[1] + d[3] + d[5] + d[7];
        return ((odd * 7 - even) % 10 + 10) % 10 == d[9] && d.Take(10).Sum() % 10 == d[10];
    }

    private static string OnlyDigits(string? value) => new((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
}

/// <summary>Firma bilgisi taşıyan istekler.</summary>
public interface ITenantProfileInput
{
    string? TaxNo { get; }
    string? TaxOffice { get; }
    string? MersisNo { get; }
    string? TradeRegistryNo { get; }
    string? Phone { get; }
    string? Email { get; }
    string? KepAddress { get; }
    string? City { get; }
    string? District { get; }
    string? Address { get; }
}
