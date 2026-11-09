using BaseForge.Core.Messaging;

namespace Kantar.Custom.Notices;

/// <summary>Firma personeline bildirim türü (platform zilinde gösterilir).</summary>
public enum TenantNoticeKind
{
    /// <summary>Çıkış tartımından sonra irsaliye eksik bilgi yüzünden taslak kaldı.</summary>
    WaybillDraftKept,

    /// <summary>e-İrsaliye alıcı ya da GİB tarafından reddedildi.</summary>
    WaybillRejected,

    /// <summary>Sevkiyat tamamlandı (son durak teslim edildi).</summary>
    ShipmentCompleted,
}

/// <summary>
/// kantar/TenantNotice — firma personeline bildirim. Platform kaydeder ve zilde gösterir; <see cref="DedupKey"/> aynı
/// olayın yeniden tesliminde ikinci kayıt açılmasını önler.
/// </summary>
public sealed class TenantNoticeEvent : IIntegrationEvent
{
    /// <summary>Olay tipi.</summary>
    public const string Type = "kantar/TenantNotice";

    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string EventType => Type;

    /// <summary>Kiracı.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Tür (<see cref="TenantNoticeKind"/> adı; platform metin olarak okur).</summary>
    public required string Kind { get; init; }

    /// <summary>Başlık.</summary>
    public required string Title { get; init; }

    /// <summary>Açıklama.</summary>
    public string? Body { get; init; }

    /// <summary>Panelde açılacak sayfa (ör. /panel/sevkiyatlar/{id}).</summary>
    public string? Link { get; init; }

    /// <summary>Tekilleştirme anahtarı.</summary>
    public required string DedupKey { get; init; }
}

/// <summary>Bildirimleri outbox'a yazar; çağıran aynı işlemde <c>SaveChanges</c> yapmalıdır.</summary>
public sealed class TenantNotices(IEventBus eventBus)
{
    /// <summary>İrsaliye taslak kaldı.</summary>
    public Task DraftKeptAsync(Guid tenantId, Guid weighingId, Guid shipmentId, string ticketNo, string stopName, string reason, CancellationToken ct)
        => eventBus.PublishAsync(new TenantNoticeEvent
        {
            TenantId = tenantId,
            Kind = nameof(TenantNoticeKind.WaybillDraftKept),
            Title = $"İrsaliye taslak kaldı: {stopName}",
            Body = $"Fiş {ticketNo} tartıldı ama irsaliye kesilemedi. {reason}",
            Link = $"/panel/sevkiyatlar/{shipmentId}",
            DedupKey = $"draft:{weighingId:N}:{stopName}",
        }, ct);

    /// <summary>e-İrsaliye reddedildi.</summary>
    public Task WaybillRejectedAsync(Guid tenantId, Guid waybillId, Guid weighingId, string waybillNo, string? message, CancellationToken ct)
        => eventBus.PublishAsync(new TenantNoticeEvent
        {
            TenantId = tenantId,
            Kind = nameof(TenantNoticeKind.WaybillRejected),
            Title = $"İrsaliye reddedildi: {waybillNo}",
            Body = string.IsNullOrWhiteSpace(message) ? "Alıcı ya da GİB irsaliyeyi reddetti; yenisini kesin." : message,
            Link = $"/panel/tartimlar/{weighingId}",
            DedupKey = $"rejected:{waybillId:N}",
        }, ct);

    /// <summary>Sevkiyat tamamlandı.</summary>
    public Task ShipmentCompletedAsync(Guid tenantId, Guid shipmentId, string shipmentNo, string plate, CancellationToken ct)
        => eventBus.PublishAsync(new TenantNoticeEvent
        {
            TenantId = tenantId,
            Kind = nameof(TenantNoticeKind.ShipmentCompleted),
            Title = $"Sevkiyat tamamlandı: {shipmentNo}",
            Body = $"{plate} son teslimatı yaptı.",
            Link = $"/panel/sevkiyatlar/{shipmentId}",
            DedupKey = $"completed:{shipmentId:N}",
        }, ct);
}
