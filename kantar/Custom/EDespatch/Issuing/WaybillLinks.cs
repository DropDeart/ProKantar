using System.Buffers.Binary;
using System.Security.Cryptography;
using Kantar.Custom.EDespatch.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>
/// Şoföre giden irsaliye linki: giriş istemeyen salt okunur sayfa (<c>{WebUrl}/irsaliye/{token}</c>). Link SMS'e
/// sığsın diye token kısadır (43 karakter): irsaliye kimliği + bitiş anı + HMAC-SHA256'nın ilk 96 biti. İmza anahtarı
/// bir kez üretilir ve veritabanında DataProtection ile şifreli saklanır (container yenilense de linkler geçerli kalır).
/// </summary>
public sealed class WaybillLinks(IServiceScopeFactory scopeFactory, IDataProtectionProvider protection, IConfiguration configuration)
{
    private const string KeyName = "waybill-link-hmac";
    private const int MacLength = 12;

    /// <summary>Linkin geçerlilik süresi.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private readonly IDataProtector _keyProtector = protection.CreateProtector("Kantar.Waybill.LinkKey.v1");
    private readonly SemaphoreSlim _keyLock = new(1, 1);
    private byte[]? _key;

    /// <summary>Linkin web adresi (<c>WaybillLinks:WebUrl</c>, varsayılan yerel arayüz).</summary>
    private string WebUrl => (configuration["WaybillLinks:WebUrl"] ?? "http://localhost:3000").TrimEnd('/');

    /// <summary>İrsaliyenin süreli linki.</summary>
    public async Task<string> UrlForAsync(Guid waybillId, DateTimeOffset issuedAt, CancellationToken ct)
    {
        var payload = new byte[20];
        waybillId.TryWriteBytes(payload.AsSpan(0, 16));
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(16), (uint)ExpiresAt(issuedAt).ToUnixTimeSeconds());
        var mac = HMACSHA256.HashData(await KeyAsync(ct), payload);
        return $"{WebUrl}/irsaliye/{WebEncoders.Base64UrlEncode([.. payload, .. mac.AsSpan(0, MacLength)])}";
    }

    /// <summary>Token'ı doğrular; bozuk, imzası tutmayan ya da süresi dolmuşsa null.</summary>
    public async Task<Guid?> TryReadAsync(string token, CancellationToken ct)
    {
        byte[] bytes;
        try
        {
            bytes = WebEncoders.Base64UrlDecode(token);
        }
        catch (FormatException)
        {
            return null;
        }

        if (bytes.Length != 20 + MacLength)
        {
            return null;
        }

        var expected = HMACSHA256.HashData(await KeyAsync(ct), bytes.AsSpan(0, 20));
        if (!CryptographicOperations.FixedTimeEquals(expected.AsSpan(0, MacLength), bytes.AsSpan(20)))
        {
            return null;
        }

        var expires = DateTimeOffset.FromUnixTimeSeconds(BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)));
        return expires < DateTimeOffset.UtcNow ? null : new Guid(bytes.AsSpan(0, 16));
    }

    /// <summary>Linkin bitiş anı.</summary>
    public static DateTimeOffset ExpiresAt(DateTimeOffset issuedAt) => issuedAt.Add(Lifetime);

    /// <summary>İmza anahtarı: yoksa üretilir; eş zamanlı iki kopyada ilk yazılan kazanır.</summary>
    private async Task<byte[]> KeyAsync(CancellationToken ct)
    {
        if (_key is { } cached)
        {
            return cached;
        }

        await _keyLock.WaitAsync(ct);
        try
        {
            if (_key is { } loaded)
            {
                return loaded;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<EDespatchDbContext>();
            var candidate = _keyProtector.Protect(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"EDespatchSecrets\" (\"Name\", \"ProtectedValue\") VALUES ({KeyName}, {candidate}) ON CONFLICT (\"Name\") DO NOTHING", ct);
            var stored = await db.Database
                .SqlQuery<string>($"SELECT \"ProtectedValue\" AS \"Value\" FROM \"EDespatchSecrets\" WHERE \"Name\" = {KeyName}")
                .SingleAsync(ct);
            _key = Convert.FromBase64String(_keyProtector.Unprotect(stored));
            return _key;
        }
        finally
        {
            _keyLock.Release();
        }
    }
}
