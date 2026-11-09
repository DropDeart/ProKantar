using System.Threading.Channels;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Platform.Custom.Messaging;
using Platform.Data;
using Platform.Entities;

namespace Platform.Custom.Notifications;

/// <summary>Bekleyen bildirim kuyruğu (kopya içi). Kayıt DB'de; kuyruk yalnız işleme sırasıdır.</summary>
public sealed class DriverNotificationQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    /// <summary>Bildirimi işlemeye alır.</summary>
    public void Enqueue(Guid notificationId) => _channel.Writer.TryWrite(notificationId);

    internal ChannelReader<Guid> Reader => _channel.Reader;
}

/// <summary>Kanal zincirinin süreleri.</summary>
public sealed class DriverNotificationOptions
{
    /// <summary>SignalR "aldım" onayı için bekleme.</summary>
    public TimeSpan AckTimeout { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// kantar/WaybillIssued: şoföre irsaliye bildirimi kaydı açar ve zinciri başlatır. Şoför yoksa ya da olay yeniden
/// teslim edildiyse (irsaliye başına tek kayıt) bir şey yapmaz.
/// </summary>
internal sealed partial class NotifyDriverOnWaybillIssued(
    PlatformDbContext db, DriverNotificationQueue queue, ILogger<NotifyDriverOnWaybillIssued> logger)
    : INotificationHandler<WaybillIssuedEvent>
{
    public async Task Handle(WaybillIssuedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (notification.DriverId is not { } driverId
            || await db.DriverNotifications.AnyAsync(n => n.WaybillId == notification.WaybillId, cancellationToken))
        {
            return;
        }

        var driver = await db.Drivers.AsNoTracking()
            .Where(d => d.Id == driverId)
            .Select(d => new { d.UserId, d.Phone })
            .FirstOrDefaultAsync(cancellationToken);
        if (driver is null)
        {
            LogNoDriver(logger, notification.WaybillNo, driverId);
            return;
        }

        var row = new DriverNotification
        {
            TenantId = notification.TenantId,
            DriverId = driverId,
            UserId = driver.UserId,
            Phone = string.IsNullOrWhiteSpace(driver.Phone) ? null : driver.Phone,
            WaybillId = notification.WaybillId,
            WaybillNo = notification.WaybillNo,
            ShipmentId = notification.ShipmentId,
            ShipmentNo = notification.ShipmentNo,
            Plate = notification.Plate,
            DeliveryAddress = notification.DeliveryAddress,
            LinkUrl = notification.LinkUrl,
            IssuedAt = notification.IssuedAt,
        };
        db.DriverNotifications.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        queue.Enqueue(row.Id);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "İrsaliye {WaybillNo}: şoför profili {DriverId} bulunamadı; bildirim yok.")]
    private static partial void LogNoDriver(ILogger logger, string waybillNo, Guid driverId);
}

/// <summary>
/// Zinciri işler: SignalR (bağlıysa, onay beklenir) → push (token varsa) → SMS (telefon varsa). İlk ulaşan kanalda durur;
/// her deneme kaydedilir. Açılışta yarım kalmış (Pending) bildirimler yeniden işlenir.
/// </summary>
internal sealed partial class DriverNotificationWorker(
    DriverNotificationQueue queue, IServiceScopeFactory scopeFactory, ILogger<DriverNotificationWorker> logger) : BackgroundService
{
    private const int MaxParallel = 16;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeuePendingAsync(stoppingToken);
        using var gate = new SemaphoreSlim(MaxParallel);
        var running = new List<Task>();
        await foreach (var id in queue.Reader.ReadAllAsync(stoppingToken))
        {
            await gate.WaitAsync(stoppingToken);
            running.RemoveAll(t => t.IsCompleted);
            running.Add(Task.Run(async () =>
            {
                try
                {
                    await ProcessAsync(id, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogFailed(logger, ex, id);
                }
                finally
                {
                    gate.Release();
                }
            }, stoppingToken));
        }

        await Task.WhenAll(running);
    }

    private async Task RequeuePendingAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            foreach (var id in await db.DriverNotifications.Where(n => n.Status == DriverNotificationStatus.Pending).Select(n => n.Id).ToListAsync(ct))
            {
                queue.Enqueue(id);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogRequeueFailed(logger, ex);
        }
    }

    private async Task ProcessAsync(Guid id, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<PlatformDbContext>();
        var n = await db.DriverNotifications.Include(x => x.Attempts).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null || n.Status != DriverNotificationStatus.Pending)
        {
            return;
        }

        if (await TrySignalRAsync(sp, db, n, ct) || await TryPushAsync(sp, db, n, ct) || await TrySmsAsync(sp, db, n, ct))
        {
            return;
        }

        n.Status = DriverNotificationStatus.Failed;
        await db.SaveChangesAsync(ct);
        await sp.GetRequiredService<TenantNotifier>().AddAsync(n.TenantId, TenantNotificationKind.DriverNotificationFailed,
            $"İrsaliye şoföre ulaşmadı: {n.WaybillNo}", $"{n.Plate} — uygulama, push ve SMS denendi; şoföre linki elle iletin.",
            n.ShipmentId is { } shipmentId ? $"/panel/sevkiyatlar/{shipmentId}" : null, $"undelivered:{n.Id:N}", ct);
    }

    private static async Task<bool> TrySignalRAsync(IServiceProvider sp, PlatformDbContext db, DriverNotification n, CancellationToken ct)
    {
        var presence = sp.GetRequiredService<DriverPresence>();
        if (!presence.IsOnline(n.UserId))
        {
            await RecordAsync(db, n, DriverNotificationChannel.SignalR, DriverNotificationResult.Skipped, "Uygulama bağlı değil.", ct);
            return false;
        }

        var acks = sp.GetRequiredService<DriverAcks>();
        var timeout = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DriverNotificationOptions>>().Value.AckTimeout;
        acks.Expect(n.Id);
        await sp.GetRequiredService<IHubContext<DriverHub>>().Clients.User(n.UserId.ToString())
            .SendAsync(DriverHub.WaybillIssued, DriverNotificationDto.From(n), ct);
        var acked = await acks.WaitAsync(n.Id, timeout, ct);
        if (!acked)
        {
            await RecordAsync(db, n, DriverNotificationChannel.SignalR, DriverNotificationResult.NoAck,
                $"{timeout.TotalSeconds:0} sn içinde onay gelmedi.", ct);
            return false;
        }

        await db.Entry(n).ReloadAsync(ct);
        await DeliveredAsync(db, n, DriverNotificationChannel.SignalR, DriverNotificationResult.Acked, null, ct);
        return true;
    }

    private static async Task<bool> TryPushAsync(IServiceProvider sp, PlatformDbContext db, DriverNotification n, CancellationToken ct)
    {
        var tokens = await db.Devices.AsNoTracking()
            .Where(d => d.DriverId == n.DriverId && d.PushToken != null && d.PushToken != string.Empty)
            .Select(d => d.PushToken!)
            .ToListAsync(ct);
        if (tokens.Count == 0)
        {
            await RecordAsync(db, n, DriverNotificationChannel.Push, DriverNotificationResult.Skipped, "Kayıtlı push token yok.", ct);
            return false;
        }

        try
        {
            var message = new PushMessage($"İrsaliye {n.WaybillNo}", $"{n.Plate} — irsaliyeniz hazır.",
                new Dictionary<string, string> { ["notificationId"] = n.Id.ToString(), ["link"] = n.LinkUrl });
            await sp.GetRequiredService<IPushSender>().SendAsync(tokens, message, ct);
            await DeliveredAsync(db, n, DriverNotificationChannel.Push, DriverNotificationResult.Sent, $"{tokens.Count} cihaz", ct);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            await RecordAsync(db, n, DriverNotificationChannel.Push, DriverNotificationResult.Failed, Trim(ex.Message), ct);
            return false;
        }
    }

    private static async Task<bool> TrySmsAsync(IServiceProvider sp, PlatformDbContext db, DriverNotification n, CancellationToken ct)
    {
        if (n.Phone is null)
        {
            await RecordAsync(db, n, DriverNotificationChannel.Sms, DriverNotificationResult.Skipped, "Şoförün telefonu yok.", ct);
            return false;
        }

        try
        {
            var text = $"ProKantar: {n.Plate} aracı için {n.WaybillNo} no.lu irsaliyeniz: {n.LinkUrl}";
            var job = await sp.GetRequiredService<ISmsGateway>().SendAsync([new SmsMessage(n.Phone, text)], ct);
            await DeliveredAsync(db, n, DriverNotificationChannel.Sms, DriverNotificationResult.Sent, job is null ? null : $"İş no {job}", ct);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            await RecordAsync(db, n, DriverNotificationChannel.Sms, DriverNotificationResult.Failed, Trim(ex.Message), ct);
            return false;
        }
    }

    private static async Task DeliveredAsync(
        PlatformDbContext db, DriverNotification n, DriverNotificationChannel channel, DriverNotificationResult result, string? detail, CancellationToken ct)
    {
        n.Status = DriverNotificationStatus.Delivered;
        n.DeliveredVia = channel;
        n.DeliveredAt = DateTimeOffset.UtcNow;
        await RecordAsync(db, n, channel, result, detail, ct);
    }

    private static async Task RecordAsync(
        PlatformDbContext db, DriverNotification n, DriverNotificationChannel channel, DriverNotificationResult result, string? detail, CancellationToken ct)
    {
        db.DriverNotificationAttempts.Add(new DriverNotificationAttempt
        {
            DriverNotificationId = n.Id, Channel = channel, Result = result, Detail = detail, At = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    private static string Trim(string message) => message.Length <= 500 ? message : message[..500];

    [LoggerMessage(Level = LogLevel.Error, Message = "Şoför bildirimi {NotificationId} işlenemedi.")]
    private static partial void LogFailed(ILogger logger, Exception exception, Guid notificationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Bekleyen şoför bildirimleri yeniden kuyruğa alınamadı.")]
    private static partial void LogRequeueFailed(ILogger logger, Exception exception);
}
