namespace Identity.Tenancy;

/// <summary>Şirket özeti ve firma bilgileri.</summary>
public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string? TaxNo,
    TenantStatus Status,
    DateTimeOffset CreatedAt,
    string? TaxOffice,
    string? MersisNo,
    string? TradeRegistryNo,
    string? Phone,
    string? Email,
    string? KepAddress,
    string? City,
    string? District,
    string? Address,
    string? ReviewNote,
    string? PostalCode)
{
    /// <summary>Entity'den.</summary>
    public static TenantDto From(Tenant t) => new(
        t.Id, t.Name, t.Slug, t.TaxNo, t.Status, t.CreatedAt, t.TaxOffice, t.MersisNo, t.TradeRegistryNo,
        t.Phone, t.Email, t.KepAddress, t.City, t.District, t.Address, t.ReviewNote, t.PostalCode);
}

/// <summary>Platform yöneticisinin başvuru listesi satırı.</summary>
public sealed record AdminTenantDto(TenantDto Tenant, string? ApplicantEmail, string? ApplicantName, bool DocumentsComplete);

/// <summary>Belge kaydı (dosya içeriği ayrı uçtan).</summary>
public sealed record TenantDocumentDto(Guid Id, TenantDocumentType Type, string FileName, string ContentType, long Size, DateTimeOffset UploadedAt)
{
    /// <summary>Entity'den.</summary>
    public static TenantDocumentDto From(TenantDocument d) => new(d.Id, d.Type, d.FileName, d.ContentType, d.Size, d.UploadedAt);
}

/// <summary>Kullanıcının bir şirketle ilişkisi (üyelik, davet ya da istek).</summary>
public sealed record MyMembershipDto(Guid MembershipId, TenantDto Tenant, MembershipStatus Status, IReadOnlyList<string> Roles, bool IsDefault);

/// <summary>Şirket yöneticisinin gördüğü üye/davet/istek.</summary>
public sealed record MemberDto(
    Guid MembershipId,
    Guid? UserId,
    string? Email,
    string? Phone,
    string? FullName,
    MembershipStatus Status,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt);

/// <summary>Katılım kodu araması sonucu (yalnız ad).</summary>
public sealed record TenantLookupDto(string Name, string Slug);

/// <summary>Şirket başvurusu: ticaret unvanı ve firma bilgileri.</summary>
public sealed record CreateTenantRequest(
    string Name,
    string? TaxNo,
    string? TaxOffice = null,
    string? MersisNo = null,
    string? TradeRegistryNo = null,
    string? Phone = null,
    string? Email = null,
    string? KepAddress = null,
    string? City = null,
    string? District = null,
    string? Address = null,
    string? PostalCode = null) : ITenantProfileInput;

/// <summary>Şirket bilgisi güncelleme (kod/slug değişmez; katılım bağlantıları bozulmasın).</summary>
public sealed record UpdateTenantRequest(
    string Name,
    string? TaxNo,
    string? TaxOffice = null,
    string? MersisNo = null,
    string? TradeRegistryNo = null,
    string? Phone = null,
    string? Email = null,
    string? KepAddress = null,
    string? City = null,
    string? District = null,
    string? Address = null,
    string? PostalCode = null) : ITenantProfileInput;

/// <summary>Katılma isteği (şirket kodu ile).</summary>
public sealed record JoinTenantRequest(string Slug);

/// <summary>Davet: e-posta veya telefon ve şirket içi roller.</summary>
public sealed record InviteMemberRequest(string? Email, string? Phone, List<string> Roles);

/// <summary>Rol listesi.</summary>
public sealed record MemberRolesRequest(List<string> Roles);

/// <summary>Şirket onay/ret/askı kararı notu.</summary>
public sealed record ReviewTenantRequest(string? Note);
