using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BaseForge.Core.Entities;
using BaseForge.Core.Serialization;

namespace Platform.Entities;

/// <summary>Bildirimin durumu.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<DriverNotificationStatus>))]
public enum DriverNotificationStatus
{
    /// <summary>Zincir sürüyor.</summary>
    Pending,

    /// <summary>Bir kanaldan ulaştı (SignalR onayı, push ya da SMS gönderildi).</summary>
    Delivered,

    /// <summary>Hiçbir kanal ulaştıramadı.</summary>
    Failed,
}

/// <summary>Bildirim kanalı; zincir bu sırayla denenir.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<DriverNotificationChannel>))]
public enum DriverNotificationChannel
{
    /// <summary>Uygulama oturumu (platform hub'ı); şoför "aldım" onayı döner.</summary>
    SignalR,

    /// <summary>Push bildirimi (FCM).</summary>
    Push,

    /// <summary>İrsaliye linkli SMS.</summary>
    Sms,
}

/// <summary>Bir kanal denemesinin sonucu.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<DriverNotificationResult>))]
public enum DriverNotificationResult
{
    /// <summary>Uygulama onayladı.</summary>
    Acked,

    /// <summary>Uygulama bağlıydı ama süresinde onay gelmedi.</summary>
    NoAck,

    /// <summary>Gönderildi (push/SMS; teslim onayı yok).</summary>
    Sent,

    /// <summary>Kanal denenemedi (bağlı değil, push token ya da telefon yok).</summary>
    Skipped,

    /// <summary>Gönderim hata verdi.</summary>
    Failed,
}

/// <summary>
/// Şoföre giden bildirim — şimdilik kantardan gelen irsaliye (elle yazıldı; generate edilmez). Zincir:
/// SignalR (10 sn onay) → push → SMS; her deneme <see cref="DriverNotificationAttempt"/> olarak kaydedilir.
/// </summary>
public sealed class DriverNotification : BaseEntity
{
    /// <summary>İrsaliyeyi kesen firma (kantar kiracısı).</summary>
    public Guid TenantId { get; set; }

    /// <summary>Şoför profili.</summary>
    public Guid DriverId { get; set; }

    /// <summary>Şoförün kullanıcısı (hub'da kullanıcı kimliği).</summary>
    public Guid UserId { get; set; }

    /// <summary>SMS numarası (anahtar biçimi, 5XXXXXXXXX).</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>İrsaliye (kantar); bildirim irsaliye başına bir tane.</summary>
    public Guid WaybillId { get; set; }

    /// <summary>İrsaliye numarası.</summary>
    [MaxLength(30)]
    public string WaybillNo { get; set; } = string.Empty;

    /// <summary>Sevkiyat (kantar).</summary>
    public Guid? ShipmentId { get; set; }

    /// <summary>Sevkiyat numarası.</summary>
    [MaxLength(40)]
    public string? ShipmentNo { get; set; }

    /// <summary>Araç plakası.</summary>
    [MaxLength(20)]
    public string Plate { get; set; } = string.Empty;

    /// <summary>Teslim adresi.</summary>
    [MaxLength(500)]
    public string? DeliveryAddress { get; set; }

    /// <summary>Giriş istemeyen süreli irsaliye linki.</summary>
    [MaxLength(1000)]
    public string LinkUrl { get; set; } = string.Empty;

    /// <summary>İrsaliyenin düzenlenme anı.</summary>
    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>Durum.</summary>
    public DriverNotificationStatus Status { get; set; } = DriverNotificationStatus.Pending;

    /// <summary>Ulaştıran kanal.</summary>
    public DriverNotificationChannel? DeliveredVia { get; set; }

    /// <summary>Ulaşma anı.</summary>
    public DateTimeOffset? DeliveredAt { get; set; }

    /// <summary>Şoför uygulamada "aldım" dediği an (hub ya da HTTP onayı; SMS'ten sonra da gelebilir).</summary>
    public DateTimeOffset? AckedAt { get; set; }

    /// <summary>Denemeler.</summary>
    public List<DriverNotificationAttempt> Attempts { get; set; } = [];
}

/// <summary>Bildirimin bir kanal denemesi.</summary>
public sealed class DriverNotificationAttempt : BaseEntity
{
    /// <summary>Bildirim.</summary>
    public Guid DriverNotificationId { get; set; }

    /// <summary>Kanal.</summary>
    public DriverNotificationChannel Channel { get; set; }

    /// <summary>Sonuç.</summary>
    public DriverNotificationResult Result { get; set; }

    /// <summary>Açıklama (hata mesajı, SMS iş no vb.).</summary>
    [MaxLength(500)]
    public string? Detail { get; set; }

    /// <summary>Deneme anı.</summary>
    public DateTimeOffset At { get; set; }
}
