namespace Platform.Custom.Validation;

/// <summary>TC kimlik no sağlama kontrolü (NVİ algoritması).</summary>
public static class NationalIds
{
    /// <summary>11 rakam, ilk hane 0 değil; 10. ve 11. haneler kontrol haneleri.</summary>
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
