namespace Kantar.Custom.Validation;

/// <summary>TCKN'yi API yanıtları için maskeler (KVKK): yalnız ilk 3 ve son 2 hane görünür.</summary>
public static class NationalIdMask
{
    /// <summary>Maskeli değer (ör. 123******01); boşsa null.</summary>
    public static string? Mask(string? nationalId)
    {
        var value = nationalId?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value.Length <= 5
            ? new string('*', value.Length)
            : string.Concat(value.AsSpan(0, 3), new string('*', value.Length - 5), value.AsSpan(value.Length - 2));
    }
}
