using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Entities;

namespace Platform.Custom.Notifications;

/// <summary>
/// "Aldım" onayları: zincir SignalR adımında onayı bekler (<see cref="WaitAsync"/>); onay hub'dan ya da HTTP'den gelir.
/// Onay her durumda kayda <c>AckedAt</c> olarak yazılır (SMS gittikten sonra gelse de).
/// </summary>
public sealed class DriverAcks(IServiceScopeFactory scopeFactory)
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource> _waiting = new();

    /// <summary>Onayı süre dolana kadar bekler; geldiyse true.</summary>
    public async Task<bool> WaitAsync(Guid notificationId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var tcs = _waiting.GetOrAdd(notificationId, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        try
        {
            await tcs.Task.WaitAsync(timeout, cancellationToken);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        finally
        {
            _waiting.TryRemove(notificationId, out _);
        }
    }

    /// <summary>Bekleme başlamadan gelen onayı kaçırmamak için önceden kayıt (mesaj gönderilmeden çağrılır).</summary>
    public void Expect(Guid notificationId)
        => _waiting.GetOrAdd(notificationId, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

    /// <summary>Şoförün onayı: kendi bildirimiyse kaydeder ve bekleyen zinciri uyandırır; değilse yok sayar.</summary>
    public async Task<bool> AckAsync(Guid notificationId, Guid userId)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var notification = await db.DriverNotifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification is null)
        {
            return false;
        }

        notification.AckedAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        if (_waiting.TryGetValue(notificationId, out var tcs))
        {
            tcs.TrySetResult();
        }

        return true;
    }
}
