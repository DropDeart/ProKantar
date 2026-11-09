using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Custom.Controllers;
using Platform.Data;
using Platform.Entities;

namespace Platform.Custom.Notifications;

/// <summary>Firma bildirimleri tabloları (idempotent, açılışta).</summary>
public static class TenantNotificationSchema
{
    /// <summary>İdempotent DDL.</summary>
    public const string Sql = """
        CREATE TABLE IF NOT EXISTS "TenantNotifications" (
            "Id" uuid NOT NULL CONSTRAINT "PK_TenantNotifications" PRIMARY KEY,
            "TenantId" uuid NOT NULL,
            "Kind" text NOT NULL,
            "Title" character varying(200) NOT NULL,
            "Body" character varying(1000) NULL,
            "Link" character varying(300) NULL,
            "DedupKey" character varying(200) NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NULL,
            "CreatedBy" text NULL,
            "IsDeleted" boolean NOT NULL,
            "DeletedAt" timestamp with time zone NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_TenantNotifications_Dedup" ON "TenantNotifications" ("TenantId", "DedupKey");
        CREATE INDEX IF NOT EXISTS "IX_TenantNotifications_Tenant_Created" ON "TenantNotifications" ("TenantId", "CreatedAt" DESC);
        CREATE TABLE IF NOT EXISTS "TenantNotificationCursors" (
            "UserId" uuid NOT NULL,
            "TenantId" uuid NOT NULL,
            "SeenAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_TenantNotificationCursors" PRIMARY KEY ("UserId", "TenantId")
        );
        """;
}

/// <summary>Firma bildirimi yazar; aynı <c>DedupKey</c> ikinci kez yazılmaz (olay yeniden teslimi).</summary>
public sealed class TenantNotifier(PlatformDbContext db)
{
    /// <summary>Bildirimi ekler (varsa yok sayar).</summary>
    public Task AddAsync(Guid tenantId, TenantNotificationKind kind, string title, string? body, string? link, string dedupKey, CancellationToken ct)
        => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "TenantNotifications" ("Id", "TenantId", "Kind", "Title", "Body", "Link", "DedupKey", "CreatedAt", "IsDeleted")
            VALUES ({Guid.CreateVersion7()}, {tenantId}, {kind.ToString()}, {Trim(title, 200)}, {TrimOrNull(body, 1000)}, {TrimOrNull(link, 300)},
                    {Trim(dedupKey, 200)}, {DateTimeOffset.UtcNow}, false)
            ON CONFLICT ("TenantId", "DedupKey") DO NOTHING
            """, ct);

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max];

    private static string? TrimOrNull(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : Trim(value, max);
}

/// <summary>kantar/TenantNotice — tüketici sözleşmesi.</summary>
public sealed class TenantNoticeEvent : BaseForge.Core.Messaging.IIntegrationEvent
{
    /// <summary>RabbitMQ olay tipi.</summary>
    public const string Type = "kantar/TenantNotice";

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; }

    /// <inheritdoc />
    public string EventType => Type;

    /// <summary>Kiracı.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Tür adı (kantar TenantNoticeKind; <see cref="TenantNotificationKind"/> ile aynı adlar).</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Açıklama.</summary>
    public string? Body { get; init; }

    /// <summary>Panel sayfası.</summary>
    public string? Link { get; init; }

    /// <summary>Tekilleştirme anahtarı.</summary>
    public string DedupKey { get; init; } = string.Empty;
}

internal sealed class SaveTenantNotice(TenantNotifier notifier) : INotificationHandler<TenantNoticeEvent>
{
    public Task Handle(TenantNoticeEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (!Enum.TryParse<TenantNotificationKind>(notification.Kind, out var kind))
        {
            return Task.CompletedTask;
        }

        return notifier.AddAsync(notification.TenantId, kind, notification.Title, notification.Body, notification.Link,
            notification.DedupKey, cancellationToken);
    }
}

/// <summary>Bildirim görünümü.</summary>
public sealed record TenantNotificationDto(Guid Id, TenantNotificationKind Kind, string Title, string? Body, string? Link, DateTimeOffset CreatedAt, bool Unread);

/// <summary>Zil: son bildirimler ve okunmamış sayısı.</summary>
public sealed record TenantNotificationFeed(IReadOnlyList<TenantNotificationDto> Items, int Unread);

/// <summary>Firma personelinin bildirimleri (zil). Firma token'daki <c>tenant_id</c>'den.</summary>
[Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
[Route("api/notifications")]
public sealed class TenantNotificationsController(PlatformDbContext db) : CallerController
{
    /// <summary>Son bildirimler (en yeni önce) ve okunmamış sayısı.</summary>
    [HttpGet]
    public async Task<ActionResult<TenantNotificationFeed>> List([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var (userId, tenantId) = (UserId, TenantId);
        var seenAt = await db.TenantNotificationCursors.AsNoTracking()
            .Where(c => c.UserId == userId && c.TenantId == tenantId)
            .Select(c => (DateTimeOffset?)c.SeenAt)
            .FirstOrDefaultAsync(cancellationToken) ?? DateTimeOffset.MinValue;
        var rows = await db.TenantNotifications.AsNoTracking()
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(cancellationToken);
        var unread = await db.TenantNotifications.CountAsync(n => n.TenantId == tenantId && n.CreatedAt > seenAt, cancellationToken);
        return Ok(new TenantNotificationFeed(
            rows.Select(n => new TenantNotificationDto(n.Id, n.Kind, n.Title, n.Body, n.Link, n.CreatedAt, n.CreatedAt > seenAt)).ToList(), unread));
    }

    /// <summary>Zil açıldı: şimdiye kadarki bildirimler okundu sayılır.</summary>
    [HttpPost("seen")]
    public async Task<IActionResult> Seen(CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "TenantNotificationCursors" ("UserId", "TenantId", "SeenAt") VALUES ({UserId}, {TenantId}, {DateTimeOffset.UtcNow})
            ON CONFLICT ("UserId", "TenantId") DO UPDATE SET "SeenAt" = EXCLUDED."SeenAt"
            """, cancellationToken);
        return NoContent();
    }
}
