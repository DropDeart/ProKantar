using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Custom.Controllers;
using Platform.Data;
using Platform.Entities;

namespace Platform.Custom.Notifications;

/// <summary>Bildirim görünümü (panel ve şoför uygulaması).</summary>
public sealed record DriverNotificationDto(
    Guid Id, Guid WaybillId, string WaybillNo, Guid? ShipmentId, string? ShipmentNo, string Plate, string? DeliveryAddress,
    string LinkUrl, DateTimeOffset IssuedAt, DriverNotificationStatus Status, DriverNotificationChannel? DeliveredVia,
    DateTimeOffset? DeliveredAt, DateTimeOffset? AckedAt, IReadOnlyList<DriverNotificationAttemptDto> Attempts)
{
    /// <summary>Kayıttan görünüm (denemeler zaman sırasıyla).</summary>
    public static DriverNotificationDto From(DriverNotification n)
    {
        ArgumentNullException.ThrowIfNull(n);
        return new(n.Id, n.WaybillId, n.WaybillNo, n.ShipmentId, n.ShipmentNo, n.Plate, n.DeliveryAddress, n.LinkUrl, n.IssuedAt,
            n.Status, n.DeliveredVia, n.DeliveredAt, n.AckedAt,
            n.Attempts.OrderBy(a => a.At).Select(a => new DriverNotificationAttemptDto(a.Channel, a.Result, a.Detail, a.At)).ToList());
    }
}

/// <summary>Kanal denemesi.</summary>
public sealed record DriverNotificationAttemptDto(DriverNotificationChannel Channel, DriverNotificationResult Result, string? Detail, DateTimeOffset At);

/// <summary>Firmanın şoför bildirimleri (sevkiyat/irsaliye detayında hangi kanaldan ulaştığı).</summary>
[Authorize(Roles = "TenantAdmin,Operator,SuperAdmin")]
[Route("api/DriverNotifications")]
public sealed class DriverNotificationsController(PlatformDbContext db) : CallerController
{
    /// <summary>Sevkiyatın ya da irsaliyenin bildirimleri (en az biri verilmeli).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DriverNotificationDto>>> List(
        [FromQuery] Guid? shipmentId, [FromQuery] Guid? waybillId, CancellationToken cancellationToken)
    {
        if (shipmentId is null && waybillId is null)
        {
            return BadRequest();
        }

        var tenantId = TenantId;
        var rows = await db.DriverNotifications.AsNoTracking().Include(n => n.Attempts)
            .Where(n => n.TenantId == tenantId)
            .Where(n => (shipmentId == null || n.ShipmentId == shipmentId) && (waybillId == null || n.WaybillId == waybillId))
            .OrderBy(n => n.IssuedAt)
            .ToListAsync(cancellationToken);
        return Ok(rows.Select(DriverNotificationDto.From).ToList());
    }
}

/// <summary>Şoförün bildirimleri (uygulama gelen kutusu) ve HTTP ile "aldım" onayı.</summary>
[Authorize(Roles = "Driver")]
[Route("api/me/notifications")]
public sealed class MyNotificationsController(PlatformDbContext db, DriverAcks acks) : CallerController
{
    /// <summary>Son bildirimler (en yeni önce).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DriverNotificationDto>>> List([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var userId = UserId;
        var rows = await db.DriverNotifications.AsNoTracking().Include(n => n.Attempts)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.IssuedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);
        return Ok(rows.Select(DriverNotificationDto.From).ToList());
    }

    /// <summary>"Aldım" onayı (hub'a bağlı olmayan istemci için).</summary>
    [HttpPost("{id:guid}/ack")]
    public async Task<IActionResult> Ack(Guid id)
        => await acks.AckAsync(id, UserId) ? NoContent() : NotFound();
}
