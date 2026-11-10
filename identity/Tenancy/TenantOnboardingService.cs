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

    /// <summary>Başvurular, sayfalı (durum süzgeci ve arama sunucuda); duruma göre toplamlarla.</summary>
    Task<AdminTenantPage> PageAsync(AdminTenantPageQuery query, CancellationToken cancellationToken);

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
        return await ToAdminDtosAsync(tenants, cancellationToken);
    }

    public async Task<AdminTenantPage> PageAsync(AdminTenantPageQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var required = TenantDocumentRules.Required.ToList();
        var requiredCount = required.Count;
        var documents = db.Set<TenantDocument>();
        var all = db.Set<Tenant>().AsNoTracking();

        // Belgeleri tam: zorunlu türlerin hepsi yüklenmiş (aynı türden birden çok dosya bir sayılır).
        var complete = all.Where(t => documents.Where(d => d.TenantId == t.Id && required.Contains(d.Type)).Select(d => d.Type).Distinct().Count() == requiredCount);
        var incomplete = all.Where(t => documents.Where(d => d.TenantId == t.Id && required.Contains(d.Type)).Select(d => d.Type).Distinct().Count() < requiredCount);

        var byStatus = await all.GroupBy(t => t.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var awaiting = await incomplete.CountAsync(t => t.Status == TenantStatus.Pending, cancellationToken);
        var counts = Enum.GetValues<TenantStatus>().ToDictionary(s => s.ToString(), s => byStatus.FirstOrDefault(x => x.Key == s)?.Count ?? 0);
        counts[nameof(TenantStatus.Pending)] -= awaiting;
        counts["AwaitingDocuments"] = awaiting;

        var filtered = query.State switch
        {
            null or "" or "all" => all,
            "AwaitingDocuments" => incomplete.Where(t => t.Status == TenantStatus.Pending),
            nameof(TenantStatus.Pending) => complete.Where(t => t.Status == TenantStatus.Pending),
            var s when Enum.TryParse<TenantStatus>(s, out var status) => all.Where(t => t.Status == status),
            _ => throw IdentityException.Invalid("Geçersiz durum süzgeci."),
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var pattern = "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var users = db.Users;
            filtered = filtered.Where(t =>
                EF.Functions.ILike(t.Name, pattern)
                || (t.TaxNo != null && EF.Functions.ILike(t.TaxNo, pattern))
                || (t.City != null && EF.Functions.ILike(t.City, pattern))
                || users.Any(u => u.Id == t.CreatedByUserId
                    && ((u.Email != null && EF.Functions.ILike(u.Email, pattern)) || (u.FullName != null && EF.Functions.ILike(u.FullName, pattern)))));
        }

        var total = await filtered.CountAsync(cancellationToken);
        var tenants = await filtered.OrderByDescending(t => t.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new AdminTenantPage(await ToAdminDtosAsync(tenants, cancellationToken), total, page, pageSize, counts);
    }

    /// <summary>Şirketleri başvuran ve belge durumuyla yönetici satırına çevirir.</summary>
    private async Task<IReadOnlyList<AdminTenantDto>> ToAdminDtosAsync(List<Tenant> tenants, CancellationToken cancellationToken)
    {
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
