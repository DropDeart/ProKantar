namespace Identity.Tenancy;

/// <summary>Şirket başvuru belgesi; dosya gizli klasörde, kayıt veritabanında.</summary>
public sealed class TenantDocument
{
    /// <summary>Kimlik (dosya adı da budur).</summary>
    public Guid Id { get; set; }

    /// <summary>Şirket.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Belge türü.</summary>
    public TenantDocumentType Type { get; set; }

    /// <summary>Yüklenen dosyanın özgün adı.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME türü (pdf, jpeg, png).</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Bayt.</summary>
    public long Size { get; set; }

    /// <summary>Yükleyen.</summary>
    public Guid UploadedByUserId { get; set; }

    /// <summary>Yükleme zamanı.</summary>
    public DateTimeOffset UploadedAt { get; set; }
}

/// <summary>Belge türü.</summary>
public enum TenantDocumentType
{
    /// <summary>Vergi levhası (zorunlu).</summary>
    TaxCertificate,

    /// <summary>İmza sirküleri (zorunlu).</summary>
    SignatureCircular,

    /// <summary>Ticaret sicil gazetesi.</summary>
    TradeRegistryGazette,

    /// <summary>Faaliyet belgesi.</summary>
    ActivityCertificate,

    /// <summary>Diğer.</summary>
    Other,
}

/// <summary>Onay için gereken belgeler.</summary>
public static class TenantDocumentRules
{
    /// <summary>Onaydan önce yüklenmiş olmalı.</summary>
    public static readonly IReadOnlyList<TenantDocumentType> Required = [TenantDocumentType.TaxCertificate, TenantDocumentType.SignatureCircular];
}
