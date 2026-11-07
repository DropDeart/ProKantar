using Microsoft.Extensions.Options;
using Platform.Custom.Messaging;

namespace Platform.Custom.Drivers;

/// <summary>
/// Şoföre bildirim. Planlanan zincir: uygulama oturumu (SignalR) → push (FCM) → SMS (bkz. docs/IS-LISTESI.md).
/// Push gelene kadar her davet SMS ile bildirilir.
/// </summary>
public interface IDriverNotifier
{
    /// <summary>Firma daveti: kayıtlı şoföre "uygulamada kabul et", kayıtsız numaraya uygulama linkli SMS.</summary>
    Task InvitedAsync(string phone, Guid? driverUserId, string tenantName, CancellationToken cancellationToken);
}

/// <summary>Davet SMS'i. Gönderilemezse davet geçerli kalır, hata yalnız log'a yazılır.</summary>
internal sealed partial class SmsDriverNotifier(ISmsGateway sms, IOptions<NetgsmOptions> options, ILogger<SmsDriverNotifier> logger) : IDriverNotifier
{
    public async Task InvitedAsync(string phone, Guid? driverUserId, string tenantName, CancellationToken cancellationToken)
    {
        var text = driverUserId is null
            ? $"{tenantName} sizi ProKantar'a şoför olarak davet etti. Uygulamayı indirip bu numarayla kaydolun: {options.Value.AppLink}"
            : $"{tenantName} sizi ProKantar'da şoför olarak davet etti. Uygulamada Davetler bölümünden kabul edebilirsiniz.";
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
