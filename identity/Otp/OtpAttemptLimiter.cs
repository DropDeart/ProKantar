using System.Threading.RateLimiting;
using Identity.Configuration;
using Identity.ErrorHandling;

namespace Identity.Otp;

/// <summary>Kod doğrulama denemelerini hedef ve istemci IP'si başına sınırlar.</summary>
public interface IOtpAttemptLimiter
{
    /// <summary>Hedef (normalize telefon/e-posta) başına sınır; aşılırsa 429.</summary>
    void EnsureTargetAllowed(string target);

    /// <summary>İstemci IP'si başına sınır; aşılırsa 429.</summary>
    void EnsureClientAllowed(string? clientIp);
}

/// <inheritdoc cref="IOtpAttemptLimiter" />
/// <remarks>Bellek içi sabit pencere (tek instance); kalıcı sınırlar OtpService'teki DB kontrolleridir.</remarks>
internal sealed class OtpAttemptLimiter : IOtpAttemptLimiter, IDisposable
{
    private const string Message = "Çok fazla doğrulama denemesi; bir süre sonra tekrar deneyin.";

    private readonly PartitionedRateLimiter<string> _byTarget;
    private readonly PartitionedRateLimiter<string> _byClient;

    public OtpAttemptLimiter(AuthOptions authOptions)
    {
        var otp = authOptions.Otp;
        var window = TimeSpan.FromSeconds(otp.VerifyWindowSeconds);
        _byTarget = Create(otp.MaxVerifyPerTarget, window);
        _byClient = Create(otp.MaxVerifyPerIp, window);
    }

    public void EnsureTargetAllowed(string target) => Acquire(_byTarget, target);

    public void EnsureClientAllowed(string? clientIp) => Acquire(_byClient, clientIp ?? "unknown");

    public void Dispose()
    {
        _byTarget.Dispose();
        _byClient.Dispose();
    }

    private static void Acquire(PartitionedRateLimiter<string> limiter, string key)
    {
        using var lease = limiter.AttemptAcquire(key);
        if (!lease.IsAcquired)
        {
            throw IdentityException.TooManyRequests(Message);
        }
    }

    private static PartitionedRateLimiter<string> Create(int permitLimit, TimeSpan window)
        => PartitionedRateLimiter.Create<string, string>(key => RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = window, QueueLimit = 0 }));
}
