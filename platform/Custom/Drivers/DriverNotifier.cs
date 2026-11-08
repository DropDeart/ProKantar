using Microsoft.Extensions.Options;
using Platform.Custom.Messaging;

namespace Platform.Custom.Drivers;

/// <summary>Davet linkinin açılacağı web adresi (<c>Invitations__WebUrl</c>): <c>{WebUrl}/davet/{kod}</c>.</summary>
public sealed class InvitationOptions
{
    /// <summary>ProKantar web arayüzü (sonda / olmadan).</summary>
    public string WebUrl { get; set; } = "http://localhost:3000";

    /// <summary>Davet linki.</summary>
    public string LinkFor(string token) => $"{WebUrl.TrimEnd('/')}/davet/{token}";
}

/// <summary>
/// Şoföre bildirim. Planlanan zincir: uygulama oturumu (SignalR) → push (FCM) → SMS (bkz. docs/IS-LISTESI.md).
/// Push gelene kadar her davet SMS ile bildirilir.
/// </summary>
public interface IDriverNotifier
{
    /// <summary>Firma daveti: davet linkli SMS (kayıtlı da olsa, kayıtsız da olsa aynı link).</summary>
    Task InvitedAsync(string phone, string tenantName, string inviteToken, CancellationToken cancellationToken);
}

/// <summary>Davet SMS'i. Gönderilemezse davet geçerli kalır, hata yalnız log'a yazılır (link firmadan da paylaşılabilir).</summary>
internal sealed partial class SmsDriverNotifier(ISmsGateway sms, IOptions<InvitationOptions> options, ILogger<SmsDriverNotifier> logger) : IDriverNotifier
{
    public async Task InvitedAsync(string phone, string tenantName, string inviteToken, CancellationToken cancellationToken)
    {
        var text = $"{tenantName} sizi ProKantar uygulamasına davet ediyor. Katılmak için tıklayın: {options.Value.LinkFor(inviteToken)}";
        try
        {
            await sms.SendAsync([new SmsMessage(phone, text)], cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            LogFailed(logger, ex, Validation.Phones.Mask(phone));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Davet SMS'i gönderilemedi ({Phone}); davet kaydedildi.")]
    private static partial void LogFailed(ILogger logger, Exception exception, string phone);
}
