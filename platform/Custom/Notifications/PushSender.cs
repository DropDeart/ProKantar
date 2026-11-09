namespace Platform.Custom.Notifications;

/// <summary>Push mesajı.</summary>
/// <param name="Title">Başlık.</param>
/// <param name="Body">Metin.</param>
/// <param name="Data">Uygulamaya giden veri (bildirim kimliği, link).</param>
public sealed record PushMessage(string Title, string Body, IReadOnlyDictionary<string, string> Data);

/// <summary>Push sağlayıcısı (FCM). Gönderim hatasında istisna atar; zincir SMS'e geçer.</summary>
public interface IPushSender
{
    /// <summary>Cihaz token'larına gönderir.</summary>
    Task SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken cancellationToken);
}

/// <summary>FCM bağlanana kadar log'a yazar (yalnız geliştirme; cihazda bildirim çıkmaz).</summary>
internal sealed partial class LogPushSender(ILogger<LogPushSender> logger) : IPushSender
{
    public Task SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken cancellationToken)
    {
        LogPush(logger, tokens.Count, message.Title, message.Body);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[PUSH:log] {Count} cihaz — {Title}: {Body}")]
    private static partial void LogPush(ILogger logger, int count, string title, string body);
}
