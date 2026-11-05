namespace Kantar.Custom.EDespatch.Providers.ELogo;

/// <summary>e-Logo adresleri (<c>EDespatch:ELogo</c> bölümü; ortam değişkeniyle ezilebilir).</summary>
public sealed class ELogoOptions
{
    /// <summary>Yapılandırma bölümü.</summary>
    public const string Section = "EDespatch:ELogo";

    /// <summary>Demo/test PostBoxService adresi.</summary>
    public Uri TestUrl { get; set; } = new("https://pb-demo.elogo.com.tr/PostBoxService.svc");

    /// <summary>Canlı PostBoxService adresi. DOĞRULANMALI: e-Logo canlı hesap açılışında verilen adresle karşılaştırın.</summary>
    public Uri LiveUrl { get; set; } = new("https://pb.elogo.com.tr/PostBoxService.svc");

    /// <summary>Login'de gönderilen uygulama adı.</summary>
    public string AppName { get; set; } = "ProKantar";

    /// <summary>İstek başına zaman aşımı.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Ortama göre adres.</summary>
    public Uri UrlFor(EDespatchEnvironment environment) => environment == EDespatchEnvironment.Live ? LiveUrl : TestUrl;
}
