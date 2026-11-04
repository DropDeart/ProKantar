using Identity.Accounts;
using Identity.Configuration;
using Identity.Entities;
using Identity.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Controllers;

/// <summary>Kullanıcının şirketleri: açma, katılma, davet yanıtı, varsayılan, ayrılma.</summary>
[ApiController]
[Route("api/tenants")]
[RequireFeature(AuthFeature.MultiTenancy)]
[Authorize(AuthenticationSchemes = AuthSchemes.CookieOrBearer)]
public sealed class TenantsApiController(
    UserManager<ApplicationUser> userManager,
    IMembershipService memberships,
    ITenantOnboardingService onboarding,
    ITenantDocumentService documents,
    IAuthorizationService authorization) : ControllerBase
{
    /// <summary>Üyelikler, davetler ve bekleyen istekler.</summary>
    [HttpGet("mine")]
    public async Task<IReadOnlyList<MyMembershipDto>> Mine(CancellationToken cancellationToken)
        => await memberships.ListMineAsync(await userManager.GetRequiredUserAsync(User), cancellationToken);

    /// <summary>Şirket açar (onay gerekiyorsa beklemede başlar).</summary>
    [HttpPost]
    public async Task<MyMembershipDto> Create(CreateTenantRequest request, CancellationToken cancellationToken)
        => await onboarding.CreateAsync(userManager.GetRequiredUserId(User), request, cancellationToken);

    /// <summary>Şirket ve firma bilgileri (şirket yöneticisi ya da platform yöneticisi).</summary>
    [HttpGet("{tenantId:guid}")]
    public async Task<TenantDto> Get(Guid tenantId, CancellationToken cancellationToken)
        => await onboarding.GetAsync(tenantId, await ActorAsync(), cancellationToken);

    /// <summary>Firma bilgilerini günceller (onay beklerken de).</summary>
    [HttpPut("{tenantId:guid}")]
    public async Task<TenantDto> Update(Guid tenantId, UpdateTenantRequest request, CancellationToken cancellationToken)
        => await onboarding.UpdateAsync(tenantId, await ActorAsync(), request, cancellationToken);

    /// <summary>Reddedilen başvuruyu yeniden incelemeye gönderir.</summary>
    [HttpPost("{tenantId:guid}/resubmit")]
    public async Task<TenantDto> Resubmit(Guid tenantId, CancellationToken cancellationToken)
        => await onboarding.ResubmitAsync(tenantId, await ActorAsync(), cancellationToken);

    /// <summary>Başvuru belgeleri.</summary>
    [HttpGet("{tenantId:guid}/documents")]
    public async Task<IReadOnlyList<TenantDocumentDto>> Documents(Guid tenantId, CancellationToken cancellationToken)
        => await documents.ListAsync(tenantId, await ActorAsync(), cancellationToken);

    /// <summary>Belge yükler (PDF/JPG/PNG, en fazla 10 MB; aynı türdeki eskisinin yerine geçer).</summary>
    [HttpPost("{tenantId:guid}/documents")]
    [RequestSizeLimit(TenantDocumentService.MaxBytes + 64 * 1024)]
    public async Task<TenantDocumentDto> Upload(Guid tenantId, [FromForm] TenantDocumentType type, IFormFile file, CancellationToken cancellationToken)
        => await documents.UploadAsync(tenantId, await ActorAsync(), type, file, cancellationToken);

    /// <summary>Belge dosyası.</summary>
    [HttpGet("{tenantId:guid}/documents/{documentId:guid}/file")]
    public async Task<IActionResult> DocumentFile(Guid tenantId, Guid documentId, CancellationToken cancellationToken)
    {
        var file = await documents.OpenAsync(tenantId, documentId, await ActorAsync(), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Belgeyi siler (onay öncesi).</summary>
    [HttpDelete("{tenantId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid tenantId, Guid documentId, CancellationToken cancellationToken)
    {
        await documents.DeleteAsync(tenantId, documentId, await ActorAsync(), cancellationToken);
        return NoContent();
    }

    /// <summary>Şirket kodundan ad (katılmadan önce).</summary>
    [HttpGet("lookup/{slug}")]
    public Task<TenantLookupDto> Lookup(string slug, CancellationToken cancellationToken)
        => memberships.LookupAsync(slug, cancellationToken);

    /// <summary>Katılma isteği gönderir.</summary>
    [HttpPost("join")]
    public async Task<MyMembershipDto> Join(JoinTenantRequest request, CancellationToken cancellationToken)
        => await memberships.RequestJoinAsync(userManager.GetRequiredUserId(User), request.Slug, cancellationToken);

    /// <summary>Daveti kabul eder.</summary>
    [HttpPost("memberships/{membershipId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid membershipId, CancellationToken cancellationToken)
    {
        await memberships.AcceptAsync(userManager.GetRequiredUserId(User), membershipId, cancellationToken);
        return NoContent();
    }

    /// <summary>Daveti reddeder.</summary>
    [HttpPost("memberships/{membershipId:guid}/decline")]
    public async Task<IActionResult> Decline(Guid membershipId, CancellationToken cancellationToken)
    {
        await memberships.DeclineAsync(userManager.GetRequiredUserId(User), membershipId, cancellationToken);
        return NoContent();
    }

    /// <summary>Girişte seçilecek şirketi belirler.</summary>
    [HttpPut("memberships/{membershipId:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid membershipId, CancellationToken cancellationToken)
    {
        await memberships.SetDefaultAsync(userManager.GetRequiredUserId(User), membershipId, cancellationToken);
        return NoContent();
    }

    private Task<TenantActor> ActorAsync() => authorization.GetTenantActorAsync(userManager, User);

    /// <summary>Şirketten ayrılır ya da isteği geri çeker.</summary>
    [HttpDelete("memberships/{membershipId:guid}")]
    public async Task<IActionResult> Leave(Guid membershipId, CancellationToken cancellationToken)
    {
        await memberships.LeaveAsync(userManager.GetRequiredUserId(User), membershipId, cancellationToken);
        return NoContent();
    }
}
