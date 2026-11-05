using Kantar.Custom.EDespatch.Providers;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Settings;

/// <summary>Gönderim anında kiracının çözülmüş yapılandırması.</summary>
/// <param name="ProviderKind">Seçili yöntem.</param>
/// <param name="SeriesPrefix">Belge numarası öneki (yöntem kâğıtsa null olabilir).</param>
/// <param name="Credentials">Şifresi çözülmüş kimlik.</param>
public sealed record EDespatchTenantConfig(EDespatchProviderKind ProviderKind, string? SeriesPrefix, EDespatchCredentials Credentials);

/// <summary>Kiracı ayarlarını okur; şifreyi yalnız gönderim anında, bellekte çözer.</summary>
public sealed class EDespatchSettingsStore(EDespatchDbContext db, EDespatchSecretProtector protector)
{
    /// <summary>Kiracının kaydı (yoksa null).</summary>
    public Task<EDespatchSetting?> FindAsync(Guid tenantId, CancellationToken ct)
        => db.Settings.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

    /// <summary>Kiracının yapılandırması; ayar yoksa kâğıt irsaliye varsayılır.</summary>
    public async Task<EDespatchTenantConfig> ResolveAsync(Guid tenantId, CancellationToken ct)
    {
        var setting = await db.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
        return setting is null
            ? new EDespatchTenantConfig(EDespatchProviderKind.PaperOnly, null, EDespatchCredentials.None)
            : new EDespatchTenantConfig(setting.ProviderKind, setting.SeriesPrefix, CredentialsOf(setting));
    }

    /// <summary>Kayıttan kimlik üretir (şifre çözülemezse boş).</summary>
    public EDespatchCredentials CredentialsOf(EDespatchSetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);
        return new EDespatchCredentials(setting.Environment, setting.UserName, protector.TryUnprotect(setting.ProtectedPassword), setting.SenderAlias);
    }
}
