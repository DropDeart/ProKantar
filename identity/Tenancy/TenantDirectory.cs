using Identity.Data;
using Identity.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Identity.Tenancy;

/// <summary>Firma rehberinde bulunan, ProKantar'a kayıtlı onaylı şirket (yalnız ticari künye).</summary>
public sealed record TenantDirectoryDto(
    Guid Id, string Name, string? TaxNo, string? TaxOffice, string? Phone, string? City, string? District, string? Address);

/// <summary>
/// Şirket personelinin cari firma eklerken ProKantar'a kayıtlı firmaları bulması. Yalnız onaylı (Active) şirketler,
/// VKN/TCKN ile birebir ya da unvanla (en az 3 harf) aranır; en çok 10 sonuç ve iletişim/e-posta dışı künye döner.
/// </summary>
public interface ITenantDirectory
{
    /// <summary>Çağıranın kendi şirketi hariç eşleşen şirketler.</summary>
    Task<IReadOnlyList<TenantDirectoryDto>> SearchAsync(string query, Guid? callerTenantId, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ITenantDirectory" />
internal sealed class TenantDirectory(IdentityServiceDbContext db) : ITenantDirectory
{
    private const int MaxResults = 10;
    private const int MinNameLength = 3;

    public async Task<IReadOnlyList<TenantDirectoryDto>> SearchAsync(string query, Guid? callerTenantId, CancellationToken cancellationToken)
    {
        var text = (query ?? string.Empty).Trim();
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
        var tenants = db.Set<Tenant>().AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active && t.Id != callerTenantId);

        if (digits.Length is 10 or 11 && digits.Length == text.Count(c => !char.IsWhiteSpace(c)))
        {
            tenants = tenants.Where(t => t.TaxNo == digits);
        }
        else if (text.Length >= MinNameLength)
        {
            var pattern = $"%{text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            tenants = tenants.Where(t => EF.Functions.ILike(t.Name, pattern, "\\"));
        }
        else
        {
            throw IdentityException.Invalid($"Unvan için en az {MinNameLength} harf ya da 10/11 haneli VKN/TCKN girin.");
        }

        return await tenants
            .OrderBy(t => t.Name)
            .Take(MaxResults)
            .Select(t => new TenantDirectoryDto(t.Id, t.Name, t.TaxNo, t.TaxOffice, t.Phone, t.City, t.District, t.Address))
            .ToListAsync(cancellationToken);
    }
}
