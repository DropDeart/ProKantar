using Kantar.Custom.EDespatch.Providers;

namespace Kantar.Custom.EDespatch.Settings;

/// <summary>Kiracının e-İrsaliye yöntemi ve entegratör kimliği (kiracı başına tek satır).</summary>
public sealed class EDespatchSetting
{
    /// <summary>Kiracı (birincil anahtar).</summary>
    public Guid TenantId { get; set; }

    /// <summary>Seçilen yöntem.</summary>
    public EDespatchProviderKind ProviderKind { get; set; } = EDespatchProviderKind.PaperOnly;

    /// <summary>Entegratör ortamı.</summary>
    public EDespatchEnvironment Environment { get; set; } = EDespatchEnvironment.Test;

    /// <summary>Entegratör kullanıcı adı.</summary>
    public string? UserName { get; set; }

    /// <summary>DataProtection ile şifrelenmiş entegratör şifresi; düz şifre hiçbir yerde saklanmaz.</summary>
    public string? ProtectedPassword { get; set; }

    /// <summary>Belge numarası seri öneki (3 karakter, ör. PKN).</summary>
    public string? SeriesPrefix { get; set; }

    /// <summary>Varsayılan gönderici etiketi (urn:mail:...gb@...).</summary>
    public string? SenderAlias { get; set; }

    /// <summary>Son değişiklik zamanı.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Son değiştiren kullanıcı.</summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>DataProtection anahtar halkası satırı (container yenilense de şifreler çözülebilsin diye DB'de).</summary>
public sealed class DataProtectionKeyRow
{
    /// <summary>Kimlik.</summary>
    public int Id { get; set; }

    /// <summary>Anahtar adı.</summary>
    public string? FriendlyName { get; set; }

    /// <summary>Anahtar XML'i.</summary>
    public string Xml { get; set; } = string.Empty;
}
