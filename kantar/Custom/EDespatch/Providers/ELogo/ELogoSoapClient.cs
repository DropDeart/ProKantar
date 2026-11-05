using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Kantar.Custom.EDespatch.Providers.ELogo;

/// <summary>e-Logo SOAP çağrısının ham sonucu.</summary>
/// <param name="ResultCode">ResultType.resultCode (1 = başarılı); yanıtta yoksa null.</param>
/// <param name="Message">resultMsg ya da SOAP faultstring.</param>
/// <param name="Body">Yanıt gövdesi (ayrıştırma için).</param>
internal sealed record ELogoReply(int? ResultCode, string? Message, XElement? Body)
{
    /// <summary>SOAP Fault döndü mü?</summary>
    public bool IsFault { get; init; }

    /// <summary>Gövdede yerel adı verilen ilk elemanın değeri.</summary>
    public string? Value(string localName) => Body?.Descendants().FirstOrDefault(e => e.Name.LocalName == localName)?.Value;
}

/// <summary>
/// e-Logo PostBoxService SOAP 1.1 istemcisi: zarfı kurar, yanıtı yerel adlarla okur. İş kararı vermez.
/// </summary>
internal sealed class ELogoSoapClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>Adlandırılmış HttpClient (User-Agent ve zaman aşımı kayıtta verilir).</summary>
    public const string HttpClientName = "edespatch-elogo";

    /// <summary>Cloudflare varsayılan/boş User-Agent'ı 1010 ile reddediyor; açık bir değer gerekir.</summary>
    public const string UserAgent = "ProKantar-eIrsaliye/1.0";

    private const string DocumentTypeParam = "DOCUMENTTYPE=DESPATCHADVICE";
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Tem = "http://tempuri.org/";
    private static readonly XNamespace Arr = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
    private static readonly XNamespace Efat = "http://schemas.datacontract.org/2004/07/eFaturaWebService";

    /// <summary>Oturum açar; başarısızsa sessionID null döner.</summary>
    public async Task<(string? SessionId, string? Error)> LoginAsync(Uri endpoint, string appName, string userName, string password, CancellationToken ct)
    {
        var reply = await CallAsync(endpoint, "Login", new XElement(Tem + "Login",
            new XElement(Tem + "login",
                new XElement(Efat + "appStr", appName),
                new XElement(Efat + "passWord", password),
                new XElement(Efat + "source", "1"),
                new XElement(Efat + "userName", userName),
                new XElement(Efat + "version", "1.0"))), ct);
        var session = reply.Value("sessionID");
        var ok = string.Equals(reply.Value("LoginResult"), "true", StringComparison.OrdinalIgnoreCase);
        return ok && !string.IsNullOrWhiteSpace(session) ? (session, null) : (null, reply.Message ?? "Giriş başarısız.");
    }

    /// <summary>Oturumu kapatır; hata yutulur (oturum zaten süreyle düşer).</summary>
    public async Task LogoutAsync(Uri endpoint, string sessionId)
    {
        try
        {
            await CallAsync(endpoint, "Logout", new XElement(Tem + "Logout", new XElement(Tem + "sessionID", sessionId)), CancellationToken.None);
        }
        catch (HttpRequestException)
        {
            // Oturum sunucuda zaman aşımıyla kapanır.
        }
        catch (TaskCanceledException)
        {
            // Oturum sunucuda zaman aşımıyla kapanır.
        }
    }

    /// <summary>Alıcının e-İrsaliye posta kutusu (PK) etiketleri; kayıtlı değilse boş.</summary>
    public async Task<IReadOnlyList<string>> GetDespatchReceiverAliasesAsync(Uri endpoint, string sessionId, string taxId, CancellationToken ct)
    {
        var reply = await CallAsync(endpoint, "CheckGibUser", new XElement(Tem + "CheckGibUser",
            new XElement(Tem + "sessionID", sessionId),
            new XElement(Tem + "vknTcknList", new XElement(Arr + "string", taxId))), ct);
        return reply.Body?.Descendants().Where(e => e.Name.LocalName == "GibUserType")
            .Where(u => u.Elements().FirstOrDefault(e => e.Name.LocalName == "Identifier")?.Value == taxId)
            .SelectMany(u => u.Elements().Where(e => e.Name.LocalName == "DespatchAdvicePkList"))
            .SelectMany(list => list.Descendants().Where(e => e.Name.LocalName == "Alias"))
            .Select(e => e.Value.Trim())
            .Where(a => a.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];
    }

    /// <summary>Belgeyi <c>{ETTN}.xml</c> olarak zipleyip gönderir (imzasız: SIGNED parametresi verilmez).</summary>
    public Task<ELogoReply> SendDocumentAsync(Uri endpoint, string sessionId, string receiverAlias, Guid ettn, byte[] ublXml, CancellationToken ct)
    {
        var name = ettn.ToString("D").ToUpperInvariant();
        var zip = Zip($"{name}.xml", ublXml);
        var hash = Convert.ToHexStringLower(MD5.HashData(zip));
        var now = TurkeyTime.ToLocal(DateTimeOffset.UtcNow).ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        return CallAsync(endpoint, "SendDocument", new XElement(Tem + "SendDocument",
            new XElement(Tem + "sessionID", sessionId),
            Params(DocumentTypeParam, $"ALIAS={receiverAlias}"),
            new XElement(Tem + "document",
                new XElement(Efat + "binaryData",
                    new XElement(Efat + "Value", Convert.ToBase64String(zip)),
                    new XElement(Efat + "contentType", "application/zip")),
                new XElement(Efat + "currentDate", now),
                new XElement(Efat + "fileName", $"{name}.zip"),
                new XElement(Efat + "hash", hash))), ct);
    }

    /// <summary>Belgenin durumunu sorar (statusInfo: code, status, description).</summary>
    public Task<ELogoReply> GetDocumentStatusAsync(Uri endpoint, string sessionId, Guid ettn, CancellationToken ct)
        => CallAsync(endpoint, "GetDocumentStatus", new XElement(Tem + "GetDocumentStatus",
            new XElement(Tem + "sessionID", sessionId),
            new XElement(Tem + "uuid", ettn.ToString("D").ToUpperInvariant()),
            Params(DocumentTypeParam)), ct);

    private async Task<ELogoReply> CallAsync(Uri endpoint, string action, XElement body, CancellationToken ct)
    {
        var envelope = new XDocument(new XElement(Soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soapenv", Soap),
            new XAttribute(XNamespace.Xmlns + "tem", Tem),
            new XAttribute(XNamespace.Xmlns + "arr", Arr),
            new XAttribute(XNamespace.Xmlns + "efat", Efat),
            new XElement(Soap + "Header"),
            new XElement(Soap + "Body", body)));

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml"),
        };
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"http://tempuri.org/IPostBoxService/{action}\"");

        using var response = await httpClientFactory.CreateClient(HttpClientName).SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        XElement? responseBody;
        try
        {
            responseBody = XDocument.Parse(text).Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "Body");
        }
        catch (System.Xml.XmlException)
        {
            // SOAP dışı yanıt (ör. Cloudflare engeli, bakım sayfası).
            return new ELogoReply(null, $"e-Logo beklenmeyen yanıt verdi (HTTP {(int)response.StatusCode}).", null) { IsFault = true };
        }

        var fault = responseBody?.Elements().FirstOrDefault(e => e.Name.LocalName == "Fault");
        if (fault is not null)
        {
            var faultText = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value;
            return new ELogoReply(null, faultText ?? "SOAP hatası.", responseBody) { IsFault = true };
        }

        var reply = new ELogoReply(null, null, responseBody);
        var code = int.TryParse(reply.Value("resultCode"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var c) ? c : (int?)null;
        return reply with { ResultCode = code, Message = reply.Value("resultMsg") };
    }

    private static XElement Params(params string[] values)
        => new(Tem + "paramList", values.Select(v => new XElement(Arr + "string", v)));

    private static byte[] Zip(string entryName, byte[] content)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            stream.Write(content);
        }

        return buffer.ToArray();
    }
}
