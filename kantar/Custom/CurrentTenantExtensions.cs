using BaseForge.Core.Interfaces;

namespace Kantar.Custom;

/// <summary><see cref="ICurrentTenant"/> yardımcıları.</summary>
internal static class CurrentTenantExtensions
{
    /// <summary>İsteğin kiracısını döner; token'da <c>tenant_id</c> yoksa hata verir.</summary>
    public static Guid RequireTenantId(this ICurrentTenant currentTenant)
        => currentTenant.TenantId
           ?? throw new InvalidOperationException("İstekte tenant_id yok; kiracıya ait olay yayınlanamaz.");
}
