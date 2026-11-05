using System.Text.RegularExpressions;
using BaseForge.Core.Exceptions;

namespace Kantar.Custom.EDespatch;

/// <summary>Alan bazında kural ihlali.</summary>
public sealed record DespatchRuleError(string Field, string Message);

/// <summary>
/// Gönderimden önce GİB UBL-TR e-İrsaliye (TEMELIRSALIYE/SEVK) şematron ve XSD zorunluluklarını C#'ta denetler;
/// entegratöre gitmeden önce kullanıcıya hangi alanın eksik olduğunu Türkçe söyler.
/// </summary>
public static partial class DespatchRules
{
    /// <summary>Tüm kuralları uygular; ihlal yoksa boş liste döner.</summary>
    public static IReadOnlyList<DespatchRuleError> Check(DespatchDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var errors = new List<DespatchRuleError>();
        void Fail(string field, string message) => errors.Add(new DespatchRuleError(field, message));

        if (document.Ettn == Guid.Empty)
        {
            Fail(nameof(document.Ettn), "ETTN boş.");
        }

        CheckDocumentNo(document, Fail);
        CheckParty("Supplier", "Gönderen", document.Supplier, requireTaxOffice: true, Fail);
        CheckParty("Customer", "Alıcı", document.Customer, requireTaxOffice: false, Fail);
        if (document.Station is { } station)
        {
            if (string.IsNullOrWhiteSpace(station.Name))
            {
                Fail("Station.Name", "İstasyon adı eksik.");
            }

            CheckAddress("Station.Address", "İstasyon adresi", station.Address, requirePostalCode: false, Fail);
        }

        CheckAddress(nameof(document.DeliveryAddress), "Teslim adresi", document.DeliveryAddress, requirePostalCode: true, Fail);
        CheckTransport(document, Fail);
        CheckLines(document.Lines, Fail);
        return errors;
    }

    /// <summary>İhlal varsa alan bazında <see cref="ValidationException"/> (HTTP 400) fırlatır.</summary>
    public static void EnsureValid(DespatchDocument document)
    {
        var errors = Check(document);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors
                .GroupBy(e => e.Field, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray(), StringComparer.Ordinal));
        }
    }

    /// <summary>Plakayı GİB biçimine getirir: boşluk/tire atılır, büyük harf.</summary>
    public static string NormalizePlate(string? plate)
        => string.Concat((plate ?? string.Empty).Where(char.IsLetterOrDigit)).ToUpperInvariant();

    private static void CheckDocumentNo(DespatchDocument document, Action<string, string> fail)
    {
        if (string.IsNullOrWhiteSpace(document.DocumentNo))
        {
            fail(nameof(document.DocumentNo), "Belge numarası eksik.");
            return;
        }

        if (!DocumentNoPattern().IsMatch(document.DocumentNo))
        {
            fail(nameof(document.DocumentNo), "Belge numarası 'ABC2026000000001' biçiminde olmalı (3 karakter seri + yıl + 9 hane).");
            return;
        }

        var year = TurkeyTime.ToLocal(document.IssuedAt).Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.Equals(document.DocumentNo.Substring(3, 4), year, StringComparison.Ordinal))
        {
            fail(nameof(document.DocumentNo), "Belge numarasındaki yıl düzenleme yılıyla aynı olmalı.");
        }
    }

    private static void CheckParty(string field, string label, DespatchParty? party, bool requireTaxOffice, Action<string, string> fail)
    {
        if (party is null)
        {
            fail(field, $"{label} bilgisi eksik.");
            return;
        }

        var kind = TaxIdChecks.Classify(party.TaxId);
        if (kind == TaxIdKind.Invalid)
        {
            fail($"{field}.TaxId", $"{label} VKN/TCKN geçersiz (VKN 10, TCKN 11 hane ve sağlaması tutmalı).");
        }

        if (string.IsNullOrWhiteSpace(party.Name))
        {
            fail($"{field}.Name", $"{label} unvanı eksik.");
        }

        if (kind == TaxIdKind.Tckn && PersonName.Split(party) is null)
        {
            fail($"{field}.Name", $"{label} TCKN'li şahıs; ad ve soyad ayrı ayrı girilmeli.");
        }

        if (requireTaxOffice && kind == TaxIdKind.Vkn && string.IsNullOrWhiteSpace(party.TaxOffice))
        {
            fail($"{field}.TaxOffice", $"{label} vergi dairesi eksik.");
        }

        CheckAddress($"{field}.Address", $"{label} adresi", party.Address, requirePostalCode: false, fail);
    }

    private static void CheckAddress(string field, string label, DespatchAddress? address, bool requirePostalCode, Action<string, string> fail)
    {
        if (address is null)
        {
            fail(field, $"{label} eksik.");
            return;
        }

        if (string.IsNullOrWhiteSpace(address.City))
        {
            fail($"{field}.City", $"{label} ili eksik.");
        }

        if (string.IsNullOrWhiteSpace(address.District))
        {
            fail($"{field}.District", $"{label} ilçesi eksik.");
        }

        if (string.IsNullOrWhiteSpace(address.Country))
        {
            fail($"{field}.Country", $"{label} ülkesi eksik.");
        }

        if (string.IsNullOrWhiteSpace(address.PostalCode))
        {
            if (requirePostalCode)
            {
                fail($"{field}.PostalCode", $"{label} posta kodu eksik.");
            }
        }
        else if (!PostalCodePattern().IsMatch(address.PostalCode.Trim()))
        {
            fail($"{field}.PostalCode", $"{label} posta kodu geçersiz (5 hane, ilk iki hane 01–81).");
        }
    }

    private static void CheckTransport(DespatchDocument document, Action<string, string> fail)
    {
        if (string.IsNullOrWhiteSpace(document.Plate))
        {
            fail(nameof(document.Plate), "Araç plakası eksik.");
        }
        else if (!PlatePattern().IsMatch(NormalizePlate(document.Plate)))
        {
            fail(nameof(document.Plate), "Araç plakası geçersiz (ör. 34ABC123).");
        }

        if (!string.IsNullOrWhiteSpace(document.TrailerPlate) && !PlatePattern().IsMatch(NormalizePlate(document.TrailerPlate)))
        {
            fail(nameof(document.TrailerPlate), "Dorse plakası geçersiz (ör. 34ABC123).");
        }

        if (document.Drivers.Count == 0 && document.Carrier is null)
        {
            fail(nameof(document.Drivers), "Şoför ya da taşıyıcı firma bilgisi girilmeli.");
        }

        for (var i = 0; i < document.Drivers.Count; i++)
        {
            var driver = document.Drivers[i];
            var field = $"Drivers[{i}]";
            if (string.IsNullOrWhiteSpace(driver.FirstName))
            {
                fail($"{field}.FirstName", "Şoför adı eksik.");
            }

            if (string.IsNullOrWhiteSpace(driver.FamilyName))
            {
                fail($"{field}.FamilyName", "Şoför soyadı eksik.");
            }

            if (string.IsNullOrWhiteSpace(driver.NationalId))
            {
                fail($"{field}.NationalId", "Şoför TCKN eksik.");
            }
            else if (TaxIdChecks.Classify(driver.NationalId) != TaxIdKind.Tckn)
            {
                fail($"{field}.NationalId", "Şoför TCKN geçersiz.");
            }
        }

        if (document.Carrier is { } carrier)
        {
            if (TaxIdChecks.Classify(carrier.TaxId) == TaxIdKind.Invalid)
            {
                fail("Carrier.TaxId", "Taşıyıcı VKN/TCKN geçersiz.");
            }

            if (string.IsNullOrWhiteSpace(carrier.Name))
            {
                fail("Carrier.Name", "Taşıyıcı unvanı eksik.");
            }

            CheckAddress("Carrier.Address", "Taşıyıcı adresi", carrier.Address, requirePostalCode: false, fail);
        }
    }

    private static void CheckLines(IReadOnlyList<DespatchLine>? lines, Action<string, string> fail)
    {
        if (lines is null || lines.Count == 0)
        {
            fail(nameof(DespatchDocument.Lines), "En az bir malzeme satırı olmalı.");
            return;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i].Name))
            {
                fail($"Lines[{i}].Name", "Malzeme adı eksik.");
            }

            if (lines[i].Quantity <= 0)
            {
                fail($"Lines[{i}].Quantity", "Miktar sıfırdan büyük olmalı.");
            }
        }
    }

    [GeneratedRegex(@"^[A-Z0-9]{3}20[0-9]{2}[0-9]{9}$")]
    private static partial Regex DocumentNoPattern();

    [GeneratedRegex(@"^((0[1-9])|([1-7][0-9])|(8[0-1]))[0-9]{3}$")]
    private static partial Regex PostalCodePattern();

    [GeneratedRegex(@"^(0[1-9]|[1-7][0-9]|8[01])[A-Z]+[0-9]+$")]
    private static partial Regex PlatePattern();
}

/// <summary>Kimlik numarası türü.</summary>
internal enum TaxIdKind
{
    /// <summary>Biçim ya da sağlama hatalı.</summary>
    Invalid,

    /// <summary>Vergi kimlik numarası (10 hane).</summary>
    Vkn,

    /// <summary>T.C. kimlik numarası (11 hane).</summary>
    Tckn,
}

/// <summary>VKN/TCKN sağlama algoritmaları (yalnız bu modül için).</summary>
internal static class TaxIdChecks
{
    /// <summary>Numarayı sınıflandırır; sağlaması tutmayan <see cref="TaxIdKind.Invalid"/> olur.</summary>
    public static TaxIdKind Classify(string? value)
    {
        var digits = value?.Trim();
        if (string.IsNullOrEmpty(digits) || !digits.All(char.IsAsciiDigit))
        {
            return TaxIdKind.Invalid;
        }

        return digits.Length switch
        {
            10 when IsVkn(digits) => TaxIdKind.Vkn,
            11 when IsTckn(digits) => TaxIdKind.Tckn,
            _ => TaxIdKind.Invalid,
        };
    }

    private static bool IsVkn(string s)
    {
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var tmp = (s[i] - '0' + 9 - i) % 10;
            var v = tmp * (1 << (9 - i)) % 9;
            if (tmp != 0 && v == 0)
            {
                v = 9;
            }

            sum += v;
        }

        return (10 - sum % 10) % 10 == s[9] - '0';
    }

    private static bool IsTckn(string s)
    {
        var d = s.Select(c => c - '0').ToArray();
        if (d[0] == 0)
        {
            return false;
        }

        var odd = d[0] + d[2] + d[4] + d[6] + d[8];
        var even = d[1] + d[3] + d[5] + d[7];
        var tenth = ((odd * 7 - even) % 10 + 10) % 10;
        return tenth == d[9] && d.Take(10).Sum() % 10 == d[10];
    }
}

/// <summary>TCKN'li tarafta UBL'nin istediği ad/soyad ayrımı.</summary>
internal static class PersonName
{
    /// <summary>Açık ad/soyad verilmişse onu, yoksa unvanın son kelimesini soyad sayar; ayrılamıyorsa null.</summary>
    public static (string First, string Family)? Split(DespatchParty party)
    {
        if (!string.IsNullOrWhiteSpace(party.PersonFirstName) && !string.IsNullOrWhiteSpace(party.PersonFamilyName))
        {
            return (party.PersonFirstName.Trim(), party.PersonFamilyName.Trim());
        }

        var parts = (party.Name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length < 2 ? null : (string.Join(' ', parts[..^1]), parts[^1]);
    }
}
