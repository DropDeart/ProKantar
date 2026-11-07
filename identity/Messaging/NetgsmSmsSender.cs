using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Identity.Configuration;

namespace Identity.Messaging;

/// <summary>
/// Netgsm OTP SMS (<c>POST sms/rest/v2/otp</c>, Basic Auth). OTP paketi: tek numara, yalnız yurt içi GSM,
/// Türkçe karaktersiz ve tek boy (≤ 155) metin; 3 dakika içinde şebekeye öncelikli teslim, İYS/kara liste filtresi yok.
/// </summary>
public sealed partial class NetgsmSmsSender(HttpClient http, AuthOptions options, ILogger<NetgsmSmsSender> logger) : ISmsSender
{
    /// <summary>Alfanümerik başlıkla tek boy OTP metninin üst sınırı.</summary>
    public const int MaxLength = 155;

    private readonly NetgsmOptions _netgsm = options.Messaging.Netgsm;

    /// <inheritdoc />
    public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        var text = ToAscii(message);
        if (text.Length > MaxLength)
        {
            throw new InvalidOperationException($"OTP SMS metni {MaxLength} karakteri aşıyor ({text.Length}).");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "sms/rest/v2/otp")
        {
            Content = JsonContent.Create(new OtpRequest(_netgsm.MsgHeader, text, ToLocalGsm(phoneNumber), _netgsm.AppName)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_netgsm.Usercode}:{_netgsm.Password}")));

        using var response = await http.SendAsync(request, cancellationToken);
        // Netgsm hata cevabını 406 ile döner; gövde yine JSON.
        OtpResponse? result;
        try
        {
            result = await response.Content.ReadFromJsonAsync<OtpResponse>(cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            result = null;
        }

        if (result?.Code != "00")
        {
            var code = result?.Code ?? $"HTTP {(int)response.StatusCode}";
            LogFailed(logger, code, result?.Description);
            throw new InvalidOperationException($"Netgsm OTP SMS gönderilemedi: {code} — {Describe(result?.Code)}");
        }

        LogSent(logger, result.JobId);
    }

    /// <summary>E.164 (+905XXXXXXXXX), 905…, 05… biçimlerini Netgsm'in beklediği 5XXXXXXXXX'e çevirir.</summary>
    public static string ToLocalGsm(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("90", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }
        else if (digits.Length == 11 && digits[0] == '0')
        {
            digits = digits[1..];
        }

        return digits.Length == 10 && digits[0] == '5'
            ? digits
            : throw new InvalidOperationException("Netgsm OTP yalnız Türkiye cep numarasına (5XXXXXXXXX) gönderilir.");
    }

    /// <summary>OTP paketi Türkçe karakter kabul etmez: ç ğ ı İ ö ş ü → c g i I o s u.</summary>
    public static string ToAscii(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(c switch
            {
                'ç' => 'c', 'Ç' => 'C', 'ğ' => 'g', 'Ğ' => 'G', 'ı' => 'i', 'İ' => 'I',
                'ö' => 'o', 'Ö' => 'O', 'ş' => 's', 'Ş' => 'S', 'ü' => 'u', 'Ü' => 'U',
                _ => c,
            });
        }

        return sb.ToString();
    }

    private static string Describe(string? code) => code switch
    {
        "20" => "mesaj metni ya da boyu hatalı",
        "30" => "kullanıcı adı/şifre hatalı, API izni yok ya da IP kısıtı",
        "40" or "41" => "gönderici adı (msgheader) tanımlı değil",
        "50" or "51" or "52" => "numara hatalı",
        "60" => "hesapta OTP SMS paketi tanımlı değil",
        "70" => "parametre hatası",
        "100" => "Netgsm sistem hatası",
        _ => "bilinmeyen yanıt",
    };

    private sealed record OtpRequest(
        [property: JsonPropertyName("msgheader")] string MsgHeader,
        [property: JsonPropertyName("msg")] string Msg,
        [property: JsonPropertyName("no")] string No,
        [property: JsonPropertyName("appname"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AppName);

    private sealed record OtpResponse(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("jobid")] string? JobId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Netgsm OTP SMS gönderildi (jobid {JobId}).")]
    private static partial void LogSent(ILogger logger, string? jobId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Netgsm OTP SMS hatası {Code}: {Description}")]
    private static partial void LogFailed(ILogger logger, string code, string? description);
}
