using Identity.Configuration;
using Identity.Data;
using Identity.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Identity.Tenancy;

/// <summary>Şirket açma (self-servis) ve platform onayı.</summary>
public interface ITenantOnboardingService
{
    /// <summary>Şirketi açar; açan kişi şirket yöneticisi olur.</summary>
    Task<MyMembershipDto> CreateAsync(Guid userId, CreateTenantRequest request, CancellationToken cancellationToken);

    /// <summary>Şirket bilgilerini günceller (şirket yöneticisi ya da platform yöneticisi; askıdayken değil).</summary>
    Task<TenantDto> UpdateAsync(Guid tenantId, TenantActor actor, UpdateTenantRequest request, CancellationToken cancellationToken);

    /// <summary>Şirketler (duruma göre süzülür).</summary>
    Task<IReadOnlyList<TenantDto>> ListAsync(TenantStatus? status, CancellationToken cancellationToken);

    /// <summary>Şirket ayrıntısı (yöneticisi ya da platform yöneticisi).</summary>
    Task<TenantDto> GetAsync(Guid tenantId, TenantActor actor, CancellationToken cancellationToken);

    /// <summary>Reddedilen başvuruyu yeniden incelemeye gönderir.</summary>
    Task<TenantDto> ResubmitAsync(Guid tenantId, TenantActor actor, CancellationToken cancellationToken);

    /// <summary>Onay / ret / askı / yeniden etkinleştirme; onay zorunlu belgeleri ister.</summary>
    Task<TenantDto> ReviewAsync(Guid tenantId, TenantStatus target, Guid reviewerId, string? note, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ITenantOnboardingService" />
internal sealed class TenantOnboardingService(IdentityServiceDbContext db, ITenantAdminGuard guard, AuthOptions options, TimeProvider clock)
    : ITenantOnboardingService
{
    private readonly MultiTenancyOptions _tenancy = options.MultiTenancy;

    public async Task<MyMembershipDto> CreateAsync(Guid userId, CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var name = ValidName(request.Name);
        var profile = TenantProfile.From(request);
        var now = clock.GetUtcNow();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = await UniqueSlugAsync(name, cancellationToken),
            Status = _tenancy.RequireApproval ? TenantStatus.Pending : TenantStatus.Active,
            CreatedByUserId = userId,
            CreatedAt = now,
        };
        tenant.ApplyProfile(profile);
        var membership = new TenantMembership
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Tenant = tenant,
            UserId = userId,
            Roles = [_tenancy.AdminRole],
            Status = MembershipStatus.Active,
            IsDefault = !await db.Set<TenantMembership>().HasDefaultAsync(userId, cancellationToken),
            CreatedAt = now,
            CreatedByUserId = userId,
        };

        db.Add(tenant);
        db.Add(membership);
        await db.SaveChangesAsync(cancellationToken);
        return new MyMembershipDto(membership.Id, TenantDto.From(tenant), membership.Status, membership.Roles, membership.IsDefault);
    }

    public async Task<TenantDto> UpdateAsync(Guid tenantId, TenantActor actor, UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        await guard.EnsureCanEditAsync(tenantId, actor, cancellationToken);
        var tenant = await db.Set<Tenant>().GetAsync(tenantId, cancellationToken);
        tenant.Name = ValidName(request.Name);
        tenant.ApplyProfile(TenantProfile.From(request));
        await db.SaveChangesAsync(cancellationToken);
        return TenantDto.From(tenant);
    }

    public async Task<TenantDto> GetAsync(Guid tenantId, TenantActor actor, CancellationToken cancellationToken)
    {
        await guard.EnsureCanEditAsync(tenantId, actor, cancellationToken, allowSuspended: true);
        return TenantDto.From(await db.Set<Tenant>().AsNoTracking().GetAsync(tenantId, cancellationToken));
    }

    public async Task<TenantDto> ResubmitAsync(Guid tenantId, TenantActor actor, CancellationToken cancellationToken)
    {
        await guard.EnsureCanEditAsync(tenantId, actor, cancellationToken);
        var tenant = await db.Set<Tenant>().GetAsync(tenantId, cancellationToken);
        tenant.Resubmit();
        await db.SaveChangesAsync(cancellationToken);
        return TenantDto.From(tenant);
    }

    public async Task<IReadOnlyList<TenantDto>> ListAsync(TenantStatus? status, CancellationToken cancellationToken)
        => await db.Set<Tenant>().AsNoTracking()
            .Where(t => status == null || t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => TenantDto.From(t))
            .ToListAsync(cancellationToken);

    public async Task<TenantDto> ReviewAsync(Guid tenantId, TenantStatus target, Guid reviewerId, string? note, CancellationToken cancellationToken)
    {
        var tenant = await db.Set<Tenant>().GetAsync(tenantId, cancellationToken);
        if (target == TenantStatus.Active && tenant.Status is TenantStatus.Pending or TenantStatus.Rejected)
        {
            await EnsureRequiredDocumentsAsync(tenantId, cancellationToken);
        }

        tenant.Review(target, reviewerId, note, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return TenantDto.From(tenant);
    }

    private async Task EnsureRequiredDocumentsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var uploaded = await db.Set<TenantDocument>().AsNoTracking()
            .Where(d => d.TenantId == tenantId)
            .Select(d => d.Type)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (TenantDocumentRules.Required.Except(uploaded).Any())
        {
            throw IdentityException.Conflict("Vergi levhası ve imza sirküleri yüklenmeden şirket onaylanamaz.");
        }
    }

    private static string ValidName(string? value)
    {
        var name = value?.Trim() ?? string.Empty;
        return name.Length is >= 2 and <= 200 ? name : throw IdentityException.Invalid("Şirket adı 2-200 karakter olmalı.");
    }

    // Aynı addan ikinci şirket "-2", "-3" ... ile ayrışır.
    private async Task<string> UniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = TenancyText.Slug(name);
        if (baseSlug.Length == 0)
        {
            baseSlug = "sirket";
        }

        var taken = await db.Set<Tenant>()
            .Where(t => t.Slug == baseSlug || t.Slug.StartsWith(baseSlug + "-"))
            .Select(t => t.Slug)
            .ToListAsync(cancellationToken);

        var slug = baseSlug;
        for (var i = 2; taken.Contains(slug, StringComparer.Ordinal); i++)
        {
            slug = $"{baseSlug}-{i}";
        }

        return slug;
    }
}
