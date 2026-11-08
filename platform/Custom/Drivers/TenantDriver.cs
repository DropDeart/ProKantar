using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BaseForge.Core.Entities;
using BaseForge.Core.Serialization;

namespace Platform.Entities;

/// <summary>Firma–şoför bağının durumu.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<TenantDriverStatus>))]
public enum TenantDriverStatus
{
    /// <summary>Davet gönderildi, şoför yanıtlamadı.</summary>
    Invited,

    /// <summary>Şoför kabul etti; firmanın şoförü.</summary>
    Active,

    /// <summary>Şoför reddetti.</summary>
    Declined,

    /// <summary>Firma daveti geri çekti ya da şoförü çıkardı.</summary>
    Removed,
}

/// <summary>Davetin nereden geldiği.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<TenantDriverSource>))]
public enum TenantDriverSource
{
    /// <summary>Kayıtlı şoför aramasından.</summary>
    Search,

    /// <summary>Telefon numarasıyla (kayıtlı olmayabilir).</summary>
    Phone,

    /// <summary>Yakındaki müsait şoförlerden.</summary>
    Nearby,
}

/// <summary>
/// Firmanın şoförü / şoför daveti — "Şoförlerimiz" (elle yazıldı; generate edilmez). Davet telefona bağlıdır: şoför
/// henüz kayıtlı değilse o numarayla kaydolup giriş yapınca davet önüne düşer.
/// </summary>
public sealed class TenantDriver : BaseEntity
{
    /// <summary>Firma.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Firma adı (davet şoföre bu adla gösterilir; davet anındaki hali).</summary>
    [MaxLength(200)]
    public string TenantName { get; set; } = string.Empty;

    /// <summary>Şoför (kayıtlıysa ya da kabul edince dolar).</summary>
    public Guid? DriverId { get; set; }

    /// <summary>Davet edilen cep telefonu (5XXXXXXXXX).</summary>
    [MaxLength(10)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Durum.</summary>
    public TenantDriverStatus Status { get; set; }

    /// <summary>Kaynak.</summary>
    public TenantDriverSource Source { get; set; }

    /// <summary>Daveti gönderen kullanıcı.</summary>
    public Guid InvitedByUserId { get; set; }

    /// <summary>Şoförün yanıt zamanı.</summary>
    public DateTimeOffset? RespondedAt { get; set; }

    /// <summary>Davet linkinin kodu (<c>/davet/{kod}</c>); tahmin edilemez, SMS ve kopyalanan linkte taşınır.</summary>
    [MaxLength(32)]
    public string? InviteToken { get; set; }
}
