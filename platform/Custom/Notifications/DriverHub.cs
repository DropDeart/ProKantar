using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Platform.Custom.Notifications;

/// <summary>
/// Şoför uygulamasının canlı kanalı (<c>/hubs/driver</c>). Uygulama bağlanır, <c>waybillIssued</c> mesajını alır ve
/// <see cref="Ack"/> ile "aldım" der; onay gelmezse zincir push ve SMS'e geçer. Token WebSocket'te
/// <c>?access_token=</c> ile gelir. Birden çok platform kopyasında Redis backplane gerekir.
/// </summary>
[Authorize(Roles = "Driver")]
public sealed class DriverHub(DriverPresence presence, DriverAcks acks) : Hub
{
    /// <summary>Hub adresi.</summary>
    public const string Path = "/hubs/driver";

    /// <summary>İstemciye giden irsaliye mesajının adı.</summary>
    public const string WaybillIssued = "waybillIssued";

    /// <inheritdoc />
    public override Task OnConnectedAsync()
    {
        if (Context.UserIdentifier is { } user)
        {
            presence.Connected(user);
        }

        return base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.UserIdentifier is { } user)
        {
            presence.Disconnected(user);
        }

        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>Uygulama bildirimi aldı; bekleyen zincir SignalR'da tamamlanır.</summary>
    public Task Ack(Guid notificationId)
        => Context.UserIdentifier is { } user ? acks.AckAsync(notificationId, Guid.Parse(user)) : Task.CompletedTask;
}

/// <summary>Hub'da kullanıcı kimliği JWT'deki <c>sub</c>'tır (BaseForge claim eşlemesini kapatır).</summary>
internal sealed class SubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirst("sub")?.Value ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}

/// <summary>Hub'a bağlı kullanıcılar (kopya başına; tek kopya varsayımı).</summary>
public sealed class DriverPresence
{
    private readonly ConcurrentDictionary<string, int> _connections = new();

    /// <summary>Kullanıcının açık bağlantısı var mı?</summary>
    public bool IsOnline(Guid userId) => _connections.TryGetValue(userId.ToString(), out var count) && count > 0;

    internal void Connected(string user) => _connections.AddOrUpdate(user, 1, (_, c) => c + 1);

    internal void Disconnected(string user)
    {
        if (_connections.AddOrUpdate(user, 0, (_, c) => Math.Max(0, c - 1)) == 0)
        {
            _connections.TryRemove(new KeyValuePair<string, int>(user, 0));
        }
    }
}
