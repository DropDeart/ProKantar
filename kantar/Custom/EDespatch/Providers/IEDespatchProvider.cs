using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Custom.EDespatch.Providers;

/// <summary>Kiracının seçtiği irsaliye yöntemi. Yeni entegratör ya da GİB doğrudan entegrasyonu buraya eklenir.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<EDespatchProviderKind>))]
public enum EDespatchProviderKind
{
    /// <summary>e-İrsaliye yok; yalnız kâğıt irsaliye numarası tutulur.</summary>
    PaperOnly,

    /// <summary>UBL XML üretilir; kullanıcı GİB portalına ya da kendi entegratörüne elle yükler.</summary>
    ManualXml,

    /// <summary>e-Logo PostBoxService (SOAP) üzerinden gönderim.</summary>
    ELogo,
}

/// <summary>Entegratör ortamı.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<EDespatchEnvironment>))]
public enum EDespatchEnvironment
{
    /// <summary>Test/demo ortamı.</summary>
    Test,

    /// <summary>Canlı ortam.</summary>
    Live,
}

/// <summary>Gönderim anındaki kimlik bilgisi (şifre çözülmüş, yalnız bellekte).</summary>
/// <param name="Environment">Test ya da canlı.</param>
/// <param name="UserName">Entegratör kullanıcı adı.</param>
/// <param name="Password">Entegratör şifresi.</param>
/// <param name="SenderAlias">Varsayılan gönderici etiketi (GB); entegratör istemiyorsa yok sayılır.</param>
public sealed record EDespatchCredentials(EDespatchEnvironment Environment, string? UserName, string? Password, string? SenderAlias)
{
    /// <summary>Kimlik gerektirmeyen yöntemler için boş değer.</summary>
    public static EDespatchCredentials None { get; } = new(EDespatchEnvironment.Test, null, null, null);

    /// <summary>Şifre log'a düşmesin.</summary>
    public override string ToString() => $"{Environment} {UserName} (şifre gizli)";
}

/// <summary>Belgenin sağlayıcıdaki durumu.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<DespatchState>))]
public enum DespatchState
{
    /// <summary>Elektronik gönderim yok (kâğıt).</summary>
    NotApplicable,

    /// <summary>XML hazır; kullanıcının elle yüklemesi bekleniyor.</summary>
    AwaitingManualUpload,

    /// <summary>Entegratöre iletildi, GİB işlemi sürüyor.</summary>
    Sent,

    /// <summary>GİB/alıcıya ulaştı.</summary>
    Delivered,

    /// <summary>Alıcı reddetti.</summary>
    Rejected,

    /// <summary>Gönderim ya da işleme hatası.</summary>
    Failed,
}

/// <summary>Gönderim sonucu.</summary>
/// <param name="State">Gönderim sonrası durum.</param>
/// <param name="Message">Kullanıcıya gösterilecek açıklama.</param>
/// <param name="ProviderReference">Sağlayıcının kayıt numarası (e-Logo refId).</param>
/// <param name="ReceiverAlias">Kullanılan alıcı etiketi.</param>
public sealed record DespatchSendResult(DespatchState State, string Message, string? ProviderReference = null, string? ReceiverAlias = null)
{
    /// <summary>Sağlayıcı belgeyi kabul etti mi?</summary>
    public bool Succeeded => State is not DespatchState.Failed;
}

/// <summary>Durum sorgusu sonucu.</summary>
/// <param name="State">Normalleştirilmiş durum.</param>
/// <param name="Message">Sağlayıcının açıklaması.</param>
/// <param name="ProviderCode">Sağlayıcının ham durum kodu.</param>
public sealed record DespatchStatusResult(DespatchState State, string Message, string? ProviderCode = null);

/// <summary>Bağlantı testi sonucu.</summary>
/// <param name="Success">Bağlantı ve kimlik doğru mu?</param>
/// <param name="Message">Açıklama.</param>
public sealed record ConnectionTestResult(bool Success, string Message);

/// <summary>
/// e-İrsaliye sağlayıcısı. Uzak hata ve ret durumları sonuç nesnesiyle döner; yapılandırma eksikliği
/// <see cref="BaseForge.Core.Exceptions.ValidationException"/> fırlatır.
/// </summary>
public interface IEDespatchProvider
{
    /// <summary>Bu sağlayıcının türü (kayıtta anahtar).</summary>
    EDespatchProviderKind Kind { get; }

    /// <summary>UBL üretilmesi gerekiyor mu (kâğıt yönteminde hayır)?</summary>
    bool RequiresUbl { get; }

    /// <summary>Belgeyi gönderir (ya da gönderime hazırlar).</summary>
    Task<DespatchSendResult> SendAsync(DespatchDocument document, byte[] ublXml, EDespatchCredentials credentials, CancellationToken cancellationToken);

    /// <summary>Belgenin güncel durumunu sorar.</summary>
    Task<DespatchStatusResult> GetStatusAsync(Guid ettn, EDespatchCredentials credentials, CancellationToken cancellationToken);

    /// <summary>Kimlik bilgisini dener (e-Logo: Login + Logout).</summary>
    Task<ConnectionTestResult> TestConnectionAsync(EDespatchCredentials credentials, CancellationToken cancellationToken);
}

/// <summary>Sağlayıcıları türe göre bulur.</summary>
public interface IEDespatchProviderRegistry
{
    /// <summary>Kayıtlı türler.</summary>
    IReadOnlyCollection<EDespatchProviderKind> Kinds { get; }

    /// <summary>Türün sağlayıcısı; kayıtlı değilse hata.</summary>
    IEDespatchProvider Get(EDespatchProviderKind kind);
}
