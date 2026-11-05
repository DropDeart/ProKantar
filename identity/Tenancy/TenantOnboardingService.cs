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

    /// <summary>Başvurular: başvuran ve zorunlu belge durumu ile (duruma göre süzülür).</summary>
    Task<IReadOnlyList<AdminTenantDto>> ListAsync(TenantStatus? status, CancellationToken cancellationToken);

    /// <summary>Şirket ayrıntısı (yöneticisi ya da platform yöneticisi).</summary>
    Task<TenantDto> GetAsync(Guid tenantId, TenantActor actor, CancellationToken cancellationToken);

    /// <summary>Reddedilen başvuruyu yeniden incelemeye gönderir.</summary>
    Task<TenantDto> ResubmitAsync(Guid tenantId, TenantActor actor, CancellationToken cancellationToken);

    /// <summary>Onay / ret / askı / yeniden etkinleştirme; onay zorunlu belgeleri ister.</summary>
    Task<TenantDto> ReviewAsync(Guid tenantId, TenantStatus target, Guid reviewerId, string? note, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ITenantOnboardingService" />
internal sealed class TenantOnboardingService(
    IdentityServiceDbContext db, ITenantAdminGuard guard, ITenantReviewNotifier notifier, AuthOptions options, TimeProvider clock)
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

    public async Task<IReadOnlyList<AdminTenantDto>> ListAsync(TenantStatus? status, CancellationToken cancellationToken)
    {
        var tenants = await db.Set<Tenant>().AsNoTracking()
            .Where(t => status == null || t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
        var ids = tenants.Select(t => t.Id).ToList();
        var creatorIds = tenants.Select(t => t.CreatedByUserId).Distinct().ToList();

        var uploaded = (await db.Set<TenantDocument>().AsNoTracking()
                .Where(d => ids.Contains(d.TenantId))
                .Select(d => new { d.TenantId, d.Type })
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToLookup(d => d.TenantId, d => d.Type);
        var applicants = await db.Users.AsNoTracking()
            .Where(u => creatorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return [.. tenants.Select(t =>
        {
            applicants.TryGetValue(t.CreatedByUserId, out var applicant);
            var complete = !TenantDocumentRules.Required.Except(uploaded[t.Id]).Any();
            return new AdminTenantDto(TenantDto.From(t), applicant?.Email, applicant?.FullName, complete);
        })];
    }

    public async Task<TenantDto> ReviewAsync(Guid tenantId, TenantStatus target, Guid reviewerId, string? note, CancellationToken cancellationToken)
    {
        var tenant = await db.Set<Tenant>().GetAsync(tenantId, cancellationToken);
        if (target == TenantStatus.Active && tenant.Status is TenantStatus.Pending or TenantStatus.Rejected)
        {
            await EnsureRequiredDocumentsAsync(tenantId, cancellationToken);
        }

        if (target == TenantStatus.Rejected && string.IsNullOrWhiteSpace(note))
        {
            throw IdentityException.Invalid("Ret gerekçesi zorunlu; başvuru sahibine gösterilir.");
        }

        tenant.Review(target, reviewerId, note?.Trim(), clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        await notifier.NotifyAsync(tenant, cancellationToken);
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
