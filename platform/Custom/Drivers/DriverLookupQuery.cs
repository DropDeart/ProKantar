using BaseForge.Core.CQRS;
using Microsoft.EntityFrameworkCore;
using Platform.Data;

namespace Platform.Custom.Drivers;

/// <summary>
/// Operatörün sevkiyata şoför ataması için telefonla birebir arama. Driver listesi yalnız sahibine açık
/// olduğundan liste dönmez; numarayı bilen tek kaydın kimliğini ve adını görür.
/// </summary>
public sealed class DriverLookupQuery : IQuery<DriverLookupDto?>
{
    /// <summary>Cep telefonu (0/90 önekli ya da boşluklu olabilir).</summary>
    public string Phone { get; set; } = string.Empty;
}

/// <summary>Atama için gereken en az şoför bilgisi.</summary>
public sealed record DriverLookupDto(Guid Id, string FullName, string Phone, bool LocationConsent);

internal sealed class DriverLookupHandler(PlatformDbContext db) : IQueryHandler<DriverLookupQuery, DriverLookupDto?>
{
    public async Task<DriverLookupDto?> Handle(DriverLookupQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var phone = Normalize(request.Phone);
        if (phone.Length != 10)
        {
            return null;
        }

        return await db.Drivers.AsNoTracking()
            .Where(d => d.IsActive && d.Phone == phone)
            .Select(d => new DriverLookupDto(d.Id, d.FullName, d.Phone, d.LocationConsent))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Rakamlara indirger, baştaki 90/0'ı atar: 5XXXXXXXXX.</summary>
    private static string Normalize(string value)
    {
        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("90", StringComparison.Ordinal))
        {
            return digits[2..];
        }

        return digits.Length == 11 && digits[0] == '0' ? digits[1..] : digits;
    }
}
