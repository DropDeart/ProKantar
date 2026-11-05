using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Settings;

/// <summary>Entegratör şifresini DataProtection ile şifreler/çözer.</summary>
public sealed class EDespatchSecretProtector(IDataProtectionProvider provider)
{
    private const string Purpose = "Kantar.EDespatch.Credentials.v1";
    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    /// <summary>Düz şifreyi şifreler.</summary>
    public string Protect(string password) => _protector.Protect(password);

    /// <summary>Şifreyi çözer; anahtar kaybolmuşsa null (kullanıcı şifreyi yeniden girmeli).</summary>
    public string? TryUnprotect(string? protectedPassword)
    {
        if (string.IsNullOrEmpty(protectedPassword))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(protectedPassword);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }
}

/// <summary>
/// DataProtection anahtar halkasını <c>DataProtectionKeys</c> tablosunda tutar: container yeniden kurulunca
/// anahtar kaybolup kayıtlı şifreler çözülemez hale gelmesin.
/// </summary>
internal sealed class EDespatchKeyRepository(IServiceScopeFactory scopeFactory) : IXmlRepository
{
    /// <inheritdoc />
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EDespatchDbContext>();
        return db.DataProtectionKeys.AsNoTracking().Select(k => k.Xml).ToList().Select(XElement.Parse).ToList();
    }

    /// <inheritdoc />
    public void StoreElement(XElement element, string friendlyName)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EDespatchDbContext>();
        db.DataProtectionKeys.Add(new DataProtectionKeyRow { FriendlyName = friendlyName, Xml = element.ToString(SaveOptions.DisableFormatting) });
        db.SaveChanges();
    }
}
