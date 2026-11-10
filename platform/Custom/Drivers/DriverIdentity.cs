using System.Globalization;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Xml.Linq;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Platform.Custom.Validation;
using Platform.Data;

namespace Platform.Custom.Drivers;

/// <summary>Kimlik doğrulama ayarı (<c>Nvi:*</c>).</summary>
public sealed class NviOptions
{
    /// <summary>
    /// NVİ KPSPublic (TCKimlikNoDogrula) ile doğrulama. Kapalıyken yalnız TCKN sağlama algoritması denetlenir
    /// (geliştirme; servis yerelden erişilemeyebilir). Production'da açık olmalı.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Servis adresi.</summary>
    public string Url { get; set; } = "https://tckimlik.nvi.gov.tr/Service/KPSPublic.asmx";
}

/// <summary>TCKN + ad + soyad + doğum yılı gerçek bir kişiye ait mi?</summary>
public interface INationalIdVerifier
{
    /// <summary>Eşleşirse true; servis yanıt vermezse <see cref="NationalIdServiceException"/>.</summary>
    Task<bool> VerifyAsync(string nationalId, string firstName, string lastName, int birthYear, CancellationToken ct);
}

/// <summary>Kimlik doğrulama servisine ulaşılamadı (kullanıcıya "sonra deneyin").</summary>
public sealed class NationalIdServiceException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Yalnız sağlama algoritması (NVİ kapalıyken).</summary>
internal sealed class ChecksumNationalIdVerifier : INationalIdVerifier
{
    public Task<bool> VerifyAsync(string nationalId, string firstName, string lastName, int birthYear, CancellationToken ct)
        => Task.FromResult(NationalIds.IsValidTckn(nationalId));
}

/// <summary>NVİ KPSPublic SOAP servisi (ücretsiz; numarayı vermez, yalnız eşleşmeyi söyler).</summary>
internal sealed class NviNationalIdVerifier(HttpClient http) : INationalIdVerifier
{
    private const string Ns = "http://tckimlik.nvi.gov.tr/WS";
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<bool> VerifyAsync(string nationalId, string firstName, string lastName, int birthYear, CancellationToken ct)
    {
        if (!NationalIds.IsValidTckn(nationalId))
        {
            return false;
        }

        var body = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body>
            <TCKimlikNoDogrula xmlns="{Ns}"><TCKimlikNo>{nationalId}</TCKimlikNo><Ad>{Escape(firstName)}</Ad><Soyad>{Escape(lastName)}</Soyad><DogumYili>{birthYear.ToString(CultureInfo.InvariantCulture)}</DogumYili></TCKimlikNoDogrula>
            </soap:Body></soap:Envelope>
            """;
        using var request = new HttpRequestMessage(HttpMethod.Post, (Uri?)null) { Content = new StringContent(body, Encoding.UTF8, "text/xml") };
        request.Headers.Add("SOAPAction", $"\"{Ns}/TCKimlikNoDogrula\"");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };
        try
        {
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var xml = XDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var result = xml.Descendants(XName.Get("TCKimlikNoDogrulaResult", Ns)).FirstOrDefault()?.Value;
            return bool.TryParse(result, out var ok)
                ? ok
                : throw new NationalIdServiceException("Kimlik doğrulama servisinden beklenmeyen yanıt.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Xml.XmlException)
        {
            throw new NationalIdServiceException("Kimlik doğrulama servisine ulaşılamadı; biraz sonra tekrar deneyin.", ex);
        }
    }

    /// <summary>NVİ adları büyük harf (Türkçe) bekler.</summary>
    private static string Escape(string value) => SecurityElement.Escape(value.Trim().ToUpper(Turkish)) ?? string.Empty;
}

/// <summary>Çağıranın şoför profili özeti (TCKN maskeli).</summary>
public sealed record MyDriverDto(Guid Id, string FullName, string Phone, bool HasNationalId, string? NationalIdMasked);

/// <summary>Çağıranın şoför profili; yoksa 404.</summary>
public sealed class MyDriverQuery : IQuery<MyDriverDto>
{
    /// <summary>Kullanıcı (controller doldurur).</summary>
    public Guid UserId { get; set; }
}

/// <summary>Şoförün TCKN'sini NVİ ile doğrulayıp profile yazar. Doğum yılı ve ad yalnız doğrulama için; saklanmaz.</summary>
public sealed class SetMyNationalIdCommand : ICommand<MyDriverDto>
{
    /// <summary>Kullanıcı (controller doldurur).</summary>
    public Guid UserId { get; set; }

    /// <summary>TC kimlik no (11 hane).</summary>
    public string NationalId { get; set; } = string.Empty;

    /// <summary>Kimlikteki ad (ikinci ad dahil).</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Kimlikteki soyad.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Doğum yılı.</summary>
    public int BirthYear { get; set; }
}

internal static class MyDriver
{
    public static MyDriverDto ToDto(Entities.Driver d)
        => new(d.Id, d.FullName, d.Phone, !string.IsNullOrEmpty(d.NationalId),
            d.NationalId is { Length: 11 } id ? $"*******{id[7..]}" : null);
}

internal sealed class MyDriverHandler(PlatformDbContext db) : IQueryHandler<MyDriverQuery, MyDriverDto>
{
    public async Task<MyDriverDto> Handle(MyDriverQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException("Driver", request.UserId);
        return MyDriver.ToDto(driver);
    }
}

internal sealed class SetMyNationalIdHandler(PlatformDbContext db, INationalIdVerifier verifier) : ICommandHandler<SetMyNationalIdCommand, MyDriverDto>
{
    public async Task<MyDriverDto> Handle(SetMyNationalIdCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var nationalId = new string((request.NationalId ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (!NationalIds.IsValidTckn(nationalId))
        {
            throw new ValidationException(nameof(request.NationalId), "Geçerli bir TC kimlik no (11 hane) girin.");
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ValidationException(nameof(request.FirstName), "Kimlikteki ad ve soyadı girin.");
        }

        var thisYear = DateTime.UtcNow.Year;
        if (request.BirthYear < thisYear - 100 || request.BirthYear > thisYear - 18)
        {
            throw new ValidationException(nameof(request.BirthYear), "Geçerli bir doğum yılı girin.");
        }

        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.UserId == request.UserId, cancellationToken)
            ?? throw new ValidationException("Driver", "Önce şoför profilinizi oluşturun (telefon doğrulaması).");

        bool verified;
        try
        {
            verified = await verifier.VerifyAsync(nationalId, request.FirstName, request.LastName, request.BirthYear, cancellationToken);
        }
        catch (NationalIdServiceException ex)
        {
            throw new ValidationException(nameof(request.NationalId), ex.Message);
        }

        if (!verified)
        {
            throw new ValidationException(nameof(request.NationalId), "Kimlik bilgileri doğrulanamadı; TC kimlik no, ad, soyad ve doğum yılını kimliğinizdeki gibi girin.");
        }

        driver.NationalId = nationalId;
        await db.SaveChangesAsync(cancellationToken);
        return MyDriver.ToDto(driver);
    }
}
