using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;

namespace Tracking.Common;

/// <summary>İsteği yapan kullanıcı/kiracı yardımcıları.</summary>
internal static class CallerExtensions
{
    /// <summary>Token'daki kullanıcı (sub); yoksa 403.</summary>
    public static Guid RequireUserId(this ICurrentUser currentUser)
        => Guid.TryParse(currentUser.UserId, out var id)
            ? id
            : throw new ForbiddenException("Token'da kullanıcı kimliği yok.");

    /// <summary>Token'daki kiracı (tenant_id); yoksa 403.</summary>
    public static Guid RequireTenantId(this ICurrentTenant currentTenant)
        => currentTenant.TenantId ?? throw new ForbiddenException("Token'da kiracı (tenant_id) yok.");
}
