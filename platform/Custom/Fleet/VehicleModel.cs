using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;
using BaseForge.Core.Entities;

namespace Platform.Entities;

/// <summary>Araç yapısı (kasa/kullanım tipi).</summary>
[JsonConverter(typeof(StrictStringEnumConverter<VehicleBodyType>))]
public enum VehicleBodyType
{
    /// <summary>Çekici (dorse ile çalışır; azami ağırlık kombinasyon içindir).</summary>
    Tractor,

    /// <summary>Kamyon (kapalı/açık kasa).</summary>
    Truck,

    /// <summary>Damperli kamyon.</summary>
    Tipper,

    /// <summary>Beton mikseri.</summary>
    Mixer,

    /// <summary>Tanker.</summary>
    Tanker,

    /// <summary>Kamyonet / hafif ticari.</summary>
    Van,
}

/// <summary>
/// Araç kataloğu kaydı (elle yazıldı; generate edilmez). Değerler üretici verilerinden derlenmiş tipik değerlerdir;
/// asıl azami ağırlık ruhsattan (<see cref="VehicleSpec.RegisteredMaxGrossKg"/>), dara kantarın ölçümünden gelir.
/// Onaysız kayıt şoför/firma önerisidir, yalnız önerene görünür.
/// </summary>
public sealed class VehicleModel : BaseEntity
{
    /// <summary>Marka.</summary>
    [MaxLength(50)]
    public string Brand { get; set; } = string.Empty;

    /// <summary>Model / versiyon (ör. "F-MAX 500").</summary>
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Yapı.</summary>
    public VehicleBodyType BodyType { get; set; }

    /// <summary>Çekiş düzeni (ör. "6x4").</summary>
    [MaxLength(10)]
    public string? AxleConfig { get; set; }

    /// <summary>Dingil sayısı (çekicide yalnız çekicinin).</summary>
    public int AxleCount { get; set; }

    /// <summary>Türkiye tescilinde tipik azami yüklü ağırlık (kg); çekicide çekici + dorse kombinasyonu.</summary>
    public decimal MaxGrossKg { get; set; }

    /// <summary>Tipik boş ağırlık (kg, üstyapı dahil; çekicide yalnız çekici).</summary>
    public decimal? TypicalTareKg { get; set; }

    /// <summary>Ortalama yakıt tüketimi (L/100 km, tahmini).</summary>
    public decimal? AvgFuelLPer100Km { get; set; }

    /// <summary>Onaylı katalog kaydı mı (öneriler onaylanınca herkese açılır).</summary>
    public bool IsApproved { get; set; }

    /// <summary>Öneren kullanıcı (katalog tohumunda boş).</summary>
    public Guid? SuggestedByUserId { get; set; }

    /// <summary>Seçilebilir mi.</summary>
    public bool IsActive { get; set; } = true;
}
