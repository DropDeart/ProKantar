using BaseForge.Core.Exceptions;
using Microsoft.Extensions.Options;

namespace Kantar.Custom.EDespatch.Providers.ELogo;

/// <summary>
/// e-Logo PostBoxService ile e-İrsaliye. Her işlem kendi oturumunu açar ve kapatır. Alıcı etiketi CheckGibUser'dan
/// bulunur; alıcı kayıtlı değilse <see cref="GibAliases.DefaultDespatchReceiver"/> kullanılır. Belge imzasız gider,
/// mali mühür imzasını e-Logo koyar.
/// </summary>
internal sealed class ELogoProvider(ELogoSoapClient client, IOptions<ELogoOptions> options, ILogger<ELogoProvider> logger) : IEDespatchProvider
{
    private const int Success = 1;

    /// <inheritdoc />
    public EDespatchProviderKind Kind => EDespatchProviderKind.ELogo;

    /// <inheritdoc />
    public bool RequiresUbl => true;

    /// <inheritdoc />
    public async Task<DespatchSendResult> SendAsync(DespatchDocument document, byte[] ublXml, EDespatchCredentials credentials, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(ublXml);
        try
        {
            return await WithSessionAsync(credentials, async (endpoint, session) =>
            {
                var aliases = await client.GetDespatchReceiverAliasesAsync(endpoint, session, document.Customer.TaxId.Trim(), cancellationToken);
                var alias = aliases.Count > 0 ? aliases[0] : GibAliases.DefaultDespatchReceiver;
                var reply = await client.SendDocumentAsync(endpoint, session, alias, document.Ettn, ublXml, cancellationToken);
                return reply.ResultCode == Success
                    ? new DespatchSendResult(DespatchState.Sent, reply.Message ?? "e-Logo'ya iletildi.", reply.Value("refId"), alias)
                    : new DespatchSendResult(DespatchState.Failed, $"e-Logo belgeyi almadı: {reply.Message}", null, alias);
            }, error => new DespatchSendResult(DespatchState.Failed, error), cancellationToken);
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "e-Logo gönderimi başarısız ({Ettn}).", document.Ettn);
            return new DespatchSendResult(DespatchState.Failed, "e-Logo'ya ulaşılamadı; sonra tekrar deneyin.");
        }
    }

    /// <inheritdoc />
    public async Task<DespatchStatusResult> GetStatusAsync(Guid ettn, EDespatchCredentials credentials, CancellationToken cancellationToken)
    {
        try
        {
            return await WithSessionAsync(credentials, async (endpoint, session) =>
            {
                var reply = await client.GetDocumentStatusAsync(endpoint, session, ettn, cancellationToken);
                if (reply.ResultCode != Success)
                {
                    return new DespatchStatusResult(DespatchState.Failed, reply.Message ?? "Durum alınamadı.");
                }

                var code = reply.Value("code");
                var description = reply.Value("description") ?? string.Empty;
                return new DespatchStatusResult(MapStatus(code, description), description, code);
            }, error => new DespatchStatusResult(DespatchState.Failed, error), cancellationToken);
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "e-Logo durum sorgusu başarısız ({Ettn}).", ettn);
            return new DespatchStatusResult(DespatchState.Failed, "e-Logo'ya ulaşılamadı; sonra tekrar deneyin.");
        }
    }

    /// <inheritdoc />
    public async Task<ConnectionTestResult> TestConnectionAsync(EDespatchCredentials credentials, CancellationToken cancellationToken)
    {
        try
        {
            return await WithSessionAsync(credentials,
                (_, _) => Task.FromResult(new ConnectionTestResult(true, "e-Logo girişi başarılı.")),
                error => new ConnectionTestResult(false, error), cancellationToken);
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "e-Logo bağlantı testi başarısız.");
            return new ConnectionTestResult(false, "e-Logo'ya ulaşılamadı.");
        }
    }

    /// <summary>
    /// e-Logo durum kodunu normalleştirir. DOĞRULANMALI: e-Logo kılavuzunda e-İrsaliye kod listesi yok; kılavuz
    /// örneğinde code=50 "GIB'e gönderildi". GİB zarf kodu 1300 başarıyla tamamlandı demektir. Ham kod yine döner.
    /// </summary>
    private static DespatchState MapStatus(string? code, string description)
    {
        var text = description.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        if (code == "1300" || text.Contains("başarıyla", StringComparison.Ordinal) || text.Contains("teslim", StringComparison.Ordinal))
        {
            return DespatchState.Delivered;
        }

        if (text.Contains("reddedil", StringComparison.Ordinal)
            || System.Text.RegularExpressions.Regex.IsMatch(text, @"\b(red|ret)\b", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1)))
        {
            return DespatchState.Rejected;
        }

        return text.Contains("hata", StringComparison.Ordinal) ? DespatchState.Failed : DespatchState.Sent;
    }

    private async Task<T> WithSessionAsync<T>(EDespatchCredentials credentials, Func<Uri, string, Task<T>> action, Func<string, T> onLoginFailed, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        if (string.IsNullOrWhiteSpace(credentials.UserName) || string.IsNullOrEmpty(credentials.Password))
        {
            throw new ValidationException("UserName", "e-Logo kullanıcı adı ve şifresi kayıtlı değil.");
        }

        var settings = options.Value;
        var endpoint = settings.UrlFor(credentials.Environment);
        var (session, error) = await client.LoginAsync(endpoint, settings.AppName, credentials.UserName.Trim(), credentials.Password, ct);
        if (session is null)
        {
            return onLoginFailed($"e-Logo girişi başarısız: {error}");
        }

        try
        {
            return await action(endpoint, session);
        }
        finally
        {
            await client.LogoutAsync(endpoint, session);
        }
    }

    /// <summary>Ağ hatası ya da zaman aşımı (kullanıcı iptali değil).</summary>
    private static bool IsTransport(Exception ex, CancellationToken ct)
        => ex is HttpRequestException || (ex is TaskCanceledException && !ct.IsCancellationRequested);
}
