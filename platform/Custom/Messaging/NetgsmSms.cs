using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Platform.Custom.Messaging;

/// <summary>Gönderilecek SMS: <c>5XXXXXXXXX</c> numara + metin.</summary>
public sealed record SmsMessage(string Phone, string Text);

/// <summary>Bilgilendirme SMS'i (davet, irsaliye linki). Doğrulama kodları identity'nin OTP göndericisinden gider.</summary>
public interface ISmsGateway
{
    /// <summary>Mesajları tek istekte gönderir (her numaraya kendi metni); Netgsm iş numarasını döner.</summary>
    Task<string?> SendAsync(IReadOnlyList<SmsMessage> messages, CancellationToken cancellationToken);
}

/// <summary>Netgsm hesabı. Şifre .env'de: <c>Netgsm__Password</c>; boşsa SMS log'a yazılır.</summary>
public sealed class NetgsmOptions
{
    /// <summary>Abone numarası (850xxxxxxx…).</summary>
    public string Usercode { get; set; } = string.Empty;

    /// <summary>"API Kullanıcısı" türündeki alt kullanıcının şifresi.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Onaylı gönderici adı (3–11 karakter).</summary>
    public string MsgHeader { get; set; } = string.Empty;

    /// <summary>Netgsm raporlarında görünen uygulama adı.</summary>
    public string? AppName { get; set; } = "ProKantar";

    /// <summary>Hesap bilgileri tam mı.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Usercode) && !string.IsNullOrWhiteSpace(Password) && !string.IsNullOrWhiteSpace(MsgHeader);
}

/// <summary>
/// Netgsm toplu SMS (<c>POST sms/rest/v2/send</c>, Basic Auth): n:n gönderim, Türkçe karakter (<c>encoding=TR</c>),
/// bilgilendirme içeriği (<c>iysfilter=0</c>, İYS sorgusu yok). Ticari/kampanya içeriği bu yoldan gönderilmez.
/// </summary>
public sealed partial class NetgsmSmsGateway(HttpClient http, IOptions<NetgsmOptions> options, ILogger<NetgsmSmsGateway> logger) : ISmsGateway
{
    private readonly NetgsmOptions _netgsm = options.Value;

    /// <inheritdoc />
    public async Task<string?> SendAsync(IReadOnlyList<SmsMessage> messages, CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "sms/rest/v2/send")
        {
            Content = JsonContent.Create(new SendRequest(
                _netgsm.MsgHeader, [.. messages.Select(m => new SendItem(m.Text, m.Phone))], "TR", "0", _netgsm.AppName)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_netgsm.Usercode}:{_netgsm.Password}")));

        using var response = await http.SendAsync(request, cancellationToken);
        var result = await ReadAsync(response, cancellationToken);
        if (result?.Code is not ("00" or "01" or "02"))
        {
            var code = result?.Code ?? $"HTTP {(int)response.StatusCode}";
            LogFailed(logger, code, result?.Description);
            throw new InvalidOperationException($"Netgsm SMS gönderilemedi: {code} — {Describe(result?.Code)}");
        }

        LogSent(logger, messages.Count, result.JobId);
        return result.JobId;
    }

    // Netgsm hata cevabını 406 ile döner; gövde yine JSON.
    private static async Task<SendResponse?> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<SendResponse>(cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static string Describe(string? code) => code switch
    {
        "20" => "mesaj metni hatalı ya da çok uzun",
        "30" => "kullanıcı adı/şifre hatalı, API izni yok ya da IP kısıtı",
        "40" => "gönderici adı (msgheader) tanımlı değil",
        "50" or "51" => "İYS ayarı",
        "70" => "parametre hatası",
        "80" => "gönderim sınırı aşıldı",
        "85" => "aynı numaraya 1 dakikada 20'den fazla gönderim",
        _ => "bilinmeyen yanıt",
    };

    private sealed record SendItem(
        [property: JsonPropertyName("msg")] string Msg,
        [property: JsonPropertyName("no")] string No);

    private sealed record SendRequest(
        [property: JsonPropertyName("msgheader")] string MsgHeader,
        [property: JsonPropertyName("messages")] IReadOnlyList<SendItem> Messages,
        [property: JsonPropertyName("encoding")] string Encoding,
        [property: JsonPropertyName("iysfilter")] string IysFilter,
        [property: JsonPropertyName("appname"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AppName);

    private sealed record SendResponse(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("jobid")] string? JobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Netgsm SMS gönderildi: {Count} mesaj (jobid {JobId}).")]
    private static partial void LogSent(ILogger logger, int count, string? jobId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Netgsm SMS hatası {Code}: {Description}")]
    private static partial void LogFailed(ILogger logger, string code, string? description);
}

/// <summary>Geliştirme: Netgsm hesabı tanımlı değilse SMS'ler log'a yazılır.</summary>
public sealed partial class LogSmsGateway(ILogger<LogSmsGateway> logger) : ISmsGateway
{
    /// <inheritdoc />
    public Task<string?> SendAsync(IReadOnlyList<SmsMessage> messages, CancellationToken cancellationToken)
    {
        foreach (var m in messages)
        {
            LogSms(logger, Validation.Phones.Mask(m.Phone), m.Text);
        }

        return Task.FromResult<string?>(null);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "[DEV SMS] {Phone}: {Text}")]
    private static partial void LogSms(ILogger logger, string phone, string text);
}
