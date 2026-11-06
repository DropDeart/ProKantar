using BaseForge.API.Controllers;
using BaseForge.Core.Exceptions;

namespace Platform.Custom.Controllers;

/// <summary>Çağıranın kimliği ve firması. Platform çok kiracılı değildir; firma token'daki <c>tenant_id</c> claim'inden okunur.</summary>
public abstract class CallerController : BaseController
{
    /// <summary>Çağıran kullanıcı; token'da yoksa 403.</summary>
    protected Guid UserId => CurrentUserId ?? throw new ForbiddenException("Kullanıcı kimliği (sub) token'da bulunamadı.");

    /// <summary>Token'daki firma; yoksa <c>null</c>.</summary>
    protected Guid? TenantIdOrNull => Guid.TryParse(User.FindFirst("tenant_id")?.Value, out var id) ? id : null;

    /// <summary>Token'daki firma; yoksa 403.</summary>
    protected Guid TenantId => TenantIdOrNull ?? throw new ForbiddenException("Firma bilgisi token'da bulunamadı.");
}
