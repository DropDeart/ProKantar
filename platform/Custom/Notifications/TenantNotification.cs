using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BaseForge.Core.Entities;
using BaseForge.Core.Serialization;

namespace Platform.Entities;

/// <summary>Firma bildirimi türü.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<TenantNotificationKind>))]
public enum TenantNotificationKind
{
    /// <summary>İrsaliye eksik bilgi yüzünden taslak kaldı (kantar).</summary>
    WaybillDraftKept,

    /// <summary>e-İrsaliye reddedildi (kantar).</summary>
    WaybillRejected,

    /// <summary>Sevkiyat tamamlandı (kantar).</summary>
    ShipmentCompleted,

    /// <summary>Şoför daveti kabul etti.</summary>
    DriverInvitationAccepted,

    /// <summary>Şoför daveti reddetti.</summary>
    DriverInvitationDeclined,

    /// <summary>İrsaliye şoföre hiçbir kanaldan ulaşmadı.</summary>
    DriverNotificationFailed,
}

/// <summary>
/// Firma personeline bildirim (zil). Firmaya aittir; okunma kullanıcı başına <see cref="TenantNotificationCursor"/> ile
/// tutulur (son bakılan andan yeniler okunmamıştır). Elle yazıldı; generate edilmez.
/// </summary>
public sealed class TenantNotification : BaseEntity
{
    /// <summary>Firma.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Tür.</summary>
    public TenantNotificationKind Kind { get; set; }

    /// <summary>Başlık.</summary>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Açıklama.</summary>
    [MaxLength(1000)]
    public string? Body { get; set; }

    /// <summary>Panelde açılacak sayfa.</summary>
    [MaxLength(300)]
    public string? Link { get; set; }

    /// <summary>Tekilleştirme anahtarı (aynı olay ikinci kez yazılmaz).</summary>
    [MaxLength(200)]
    public string DedupKey { get; set; } = string.Empty;
}

/// <summary>Kullanıcının firmadaki bildirimlere en son baktığı an.</summary>
[Microsoft.EntityFrameworkCore.PrimaryKey(nameof(UserId), nameof(TenantId))]
public sealed class TenantNotificationCursor
{
    /// <summary>Kullanıcı.</summary>
    public Guid UserId { get; set; }

    /// <summary>Firma.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Son bakış.</summary>
    public DateTimeOffset SeenAt { get; set; }
}
