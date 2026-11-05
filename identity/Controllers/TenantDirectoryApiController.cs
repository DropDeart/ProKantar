using Identity.Configuration;
using Identity.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Controllers;

/// <summary>Firma rehberi: cari firma eklerken ProKantar'a kayıtlı onaylı şirketleri bulma.</summary>
[ApiController]
[Route("api/tenants/directory")]
[RequireFeature(AuthFeature.MultiTenancy)]
[Authorize(AuthenticationSchemes = AuthSchemes.CookieOrBearer, Roles = "TenantAdmin,Operator,SuperAdmin")]
public sealed class TenantDirectoryApiController(ITenantDirectory directory) : ControllerBase
{
    /// <summary>VKN/TCKN (birebir) ya da unvanla (en az 3 harf) en çok 10 şirket; çağıranın şirketi hariç.</summary>
    [HttpGet]
    public Task<IReadOnlyList<TenantDirectoryDto>> Search([FromQuery] string q, CancellationToken cancellationToken)
        => directory.SearchAsync(q, Guid.TryParse(User.FindFirst("tenant_id")?.Value, out var id) ? id : null, cancellationToken);
}
