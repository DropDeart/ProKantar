namespace Kantar.Custom.Validation;

/// <summary>VKN (10 hane) ve TCKN (11 hane) sağlama kontrolleri (GİB/NVİ algoritmaları).</summary>
public static class TaxIds
{
    /// <summary>10 haneli VKN ya da 11 haneli TCKN geçerli mi.</summary>
    public static bool IsValidTaxNo(string? value) => value?.Length switch
    {
        10 => IsValidVkn(value),
        11 => IsValidTckn(value),
        _ => false,
    };

    /// <summary>VKN: 10 rakam, son hane ilk 9 haneden hesaplanan kontrol hanesi.</summary>
    public static bool IsValidVkn(string? vkn)
    {
        if (vkn is not { Length: 10 } || !vkn.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var t = (vkn[i] - '0' + 9 - i) % 10;
            var p = t * (1 << (9 - i)) % 9;
            sum += t != 0 && p == 0 ? 9 : p;
        }

        return (10 - sum % 10) % 10 == vkn[9] - '0';
    }

    /// <summary>TCKN: 11 rakam, ilk hane 0 değil; 10. ve 11. haneler kontrol haneleri.</summary>
    public static bool IsValidTckn(string? tckn)
    {
        if (tckn is not { Length: 11 } || !tckn.All(char.IsAsciiDigit) || tckn[0] == '0')
        {
            return false;
        }

        var d = tckn.Select(c => c - '0').ToArray();
        var odd = d[0] + d[2] + d[4] + d[6] + d[8];
        var even = d[1] + d[3] + d[5] + d[7];
        return ((odd * 7 - even) % 10 + 10) % 10 == d[9] && d.Take(10).Sum() % 10 == d[10];
    }
}
