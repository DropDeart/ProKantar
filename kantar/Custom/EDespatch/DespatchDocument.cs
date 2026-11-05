namespace Kantar.Custom.EDespatch;

/// <summary>
/// Entity'lerden bağımsız e-İrsaliye belgesi. Tartım/sevkiyat verisinden doldurulur; UBL üretimi ve sağlayıcılar
/// yalnız bu modeli tanır.
/// </summary>
public sealed record DespatchDocument
{
    /// <summary>ETTN (belgenin evrensel kimliği).</summary>
    public required Guid Ettn { get; init; }

    /// <summary>Belge numarası: 3 karakter seri + yıl + 9 hane sıra (ör. ABC2026000000001).</summary>
    public required string DocumentNo { get; init; }

    /// <summary>Düzenleme anı; XML'e Türkiye saatiyle yazılır.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Fiili sevk anı; XML'e Türkiye saatiyle yazılır.</summary>
    public required DateTimeOffset DespatchedAt { get; init; }

    /// <summary>Serbest not (isteğe bağlı).</summary>
    public string? Note { get; init; }

    /// <summary>Gönderen (irsaliyeyi düzenleyen kiracı).</summary>
    public required DespatchParty Supplier { get; init; }

    /// <summary>Malın çıktığı istasyon (isteğe bağlı fiziki konum).</summary>
    public DespatchSite? Station { get; init; }

    /// <summary>Alıcı (cari firma ya da şahıs).</summary>
    public required DespatchParty Customer { get; init; }

    /// <summary>Teslim adresi.</summary>
    public required DespatchAddress DeliveryAddress { get; init; }

    /// <summary>Araç plakası.</summary>
    public required string Plate { get; init; }

    /// <summary>Dorse plakası (isteğe bağlı).</summary>
    public string? TrailerPlate { get; init; }

    /// <summary>Şoförler (taşıyıcı yoksa en az biri zorunlu).</summary>
    public IReadOnlyList<DespatchDriver> Drivers { get; init; } = [];

    /// <summary>Taşıyıcı firma (isteğe bağlı).</summary>
    public DespatchCarrier? Carrier { get; init; }

    /// <summary>Malzeme satırları.</summary>
    public required IReadOnlyList<DespatchLine> Lines { get; init; }

    /// <summary>Referans kantar fişi numarası (isteğe bağlı).</summary>
    public string? WeighingTicketNo { get; init; }
}

/// <summary>Gönderen ya da alıcı taraf.</summary>
public sealed record DespatchParty
{
    /// <summary>VKN (10 hane) ya da TCKN (11 hane).</summary>
    public required string TaxId { get; init; }

    /// <summary>Unvan; şahısta ad soyad.</summary>
    public required string Name { get; init; }

    /// <summary>TCKN'li şahısta ad (boşsa <see cref="Name"/>'den ayrılır).</summary>
    public string? PersonFirstName { get; init; }

    /// <summary>TCKN'li şahısta soyad (boşsa <see cref="Name"/>'den ayrılır).</summary>
    public string? PersonFamilyName { get; init; }

    /// <summary>Vergi dairesi.</summary>
    public string? TaxOffice { get; init; }

    /// <summary>Adres.</summary>
    public required DespatchAddress Address { get; init; }

    /// <summary>Telefon (isteğe bağlı).</summary>
    public string? Phone { get; init; }

    /// <summary>E-posta (isteğe bağlı).</summary>
    public string? Email { get; init; }
}

/// <summary>İsimli fiziki konum (istasyon).</summary>
public sealed record DespatchSite(string Name, DespatchAddress Address);

/// <summary>Posta adresi.</summary>
public sealed record DespatchAddress
{
    /// <summary>Açık adres (mahalle, cadde, no).</summary>
    public string? Street { get; init; }

    /// <summary>İlçe.</summary>
    public string? District { get; init; }

    /// <summary>İl.</summary>
    public string? City { get; init; }

    /// <summary>Posta kodu (5 hane).</summary>
    public string? PostalCode { get; init; }

    /// <summary>Ülke.</summary>
    public string Country { get; init; } = "Türkiye";
}

/// <summary>Şoför.</summary>
public sealed record DespatchDriver(string FirstName, string FamilyName, string NationalId);

/// <summary>Taşıyıcı firma.</summary>
public sealed record DespatchCarrier(string TaxId, string Name, DespatchAddress Address);

/// <summary>İrsaliye satırı.</summary>
public sealed record DespatchLine(string Name, string? Code, decimal Quantity, DespatchUnit Unit);

/// <summary>Satır birimi; UBL kodu <see cref="DespatchUnitCodes"/>'ta.</summary>
public enum DespatchUnit
{
    /// <summary>Kilogram (KGM) — kantar için önerilen varsayılan: net kg.</summary>
    Kg,

    /// <summary>Ton (TNE).</summary>
    Ton,

    /// <summary>Metreküp (MTQ).</summary>
    M3,

    /// <summary>Adet (C62).</summary>
    Piece,
}

/// <summary>Birimlerin UN/ECE Rec 20 karşılıkları.</summary>
public static class DespatchUnitCodes
{
    /// <summary>UBL <c>unitCode</c> değeri.</summary>
    public static string Of(DespatchUnit unit) => unit switch
    {
        DespatchUnit.Kg => "KGM",
        DespatchUnit.Ton => "TNE",
        DespatchUnit.M3 => "MTQ",
        DespatchUnit.Piece => "C62",
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Bilinmeyen birim."),
    };
}
