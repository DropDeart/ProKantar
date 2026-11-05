using System.Globalization;
using Identity.Configuration;
using Identity.Data;
using Identity.ErrorHandling;
using Identity.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Identity.Otp;

/// <summary>Gönderim sonucu: kod süresi ve yeniden gönderim beklemesi.</summary>
public sealed record OtpSendResult(int ExpiresInSeconds, int ResendAfterSeconds);

/// <summary>Tek kullanımlık kod gönderme ve doğrulama.</summary>
public interface IOtpService
{
    /// <summary>Kod üretir ve gönderir; bekleme/saatlik sınır aşılırsa 429.</summary>
    Task<OtpSendResult> SendAsync(OtpChannel channel, string target, OtpPurpose purpose, CancellationToken cancellationToken);

    /// <summary>Son kodu atomik olarak doğrular ve tek seferlik tüketir; her deneme sayılır, hedef başı sınır aşılırsa 429.</summary>
    Task<bool> VerifyAsync(OtpChannel channel, string target, OtpPurpose purpose, string code, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IOtpService" />
internal sealed class OtpService(
    IdentityServiceDbContext db,
    ISmsSender sms,
    IEmailSender email,
    AuthOptions authOptions,
    IOtpAttemptLimiter limiter,
    TimeProvider clock) : IOtpService
{
    private readonly OtpOptions _options = authOptions.Otp;

    private DbSet<OtpChallenge> Challenges => db.Set<OtpChallenge>();

    public async Task<OtpSendResult> SendAsync(OtpChannel channel, string target, OtpPurpose purpose, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var recent = await Challenges
            .Where(c => c.Target == target && c.Purpose == purpose && c.CreatedAt > now.AddHours(-1))
            .Select(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        if (recent.Count >= _options.MaxPerHour)
        {
            throw IdentityException.TooManyRequests("Çok fazla kod istendi; bir süre sonra tekrar deneyin.");
        }

        var wait = recent.Count == 0 ? 0 : (int)Math.Ceiling((recent.Max().AddSeconds(_options.ResendCooldownSeconds) - now).TotalSeconds);
        if (wait > 0)
        {
            throw IdentityException.TooManyRequests($"Yeni kod için {wait} saniye bekleyin.");
        }

        var code = OtpCode.Generate(_options.CodeLength);
        Challenges.Add(new OtpChallenge
        {
            Id = Guid.NewGuid(),
            Channel = channel,
            Target = target,
            Purpose = purpose,
            CodeHash = OtpCode.Hash(code),
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(_options.LifetimeSeconds),
        });
        await db.SaveChangesAsync(cancellationToken);

        var minutes = Math.Max(1, _options.LifetimeSeconds / 60);
        if (channel == OtpChannel.Sms)
        {
            var text = string.Format(CultureInfo.InvariantCulture, "Doğrulama kodunuz: {0}. {1} dakika geçerlidir; kimseyle paylaşmayın.", code, minutes);
            await sms.SendAsync(target, text, cancellationToken);
        }
        else
        {
            await email.SendAsync(EmailTemplate.Build(target, "Doğrulama kodu", authOptions.Messaging.Smtp.FromName, "Doğrulama kodunuz",
                [$"Kod {minutes} dakika geçerlidir. Bu isteği siz yapmadıysanız e-postayı yok sayın; kodu kimseyle paylaşmayın."],
                highlight: code), cancellationToken);
        }

        return new OtpSendResult(_options.LifetimeSeconds, _options.ResendCooldownSeconds);
    }

    public async Task<bool> VerifyAsync(OtpChannel channel, string target, OtpPurpose purpose, string code, CancellationToken cancellationToken)
    {
        limiter.EnsureTargetAllowed(target);
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        // Yalnız son gönderilen kod geçerlidir; eski kodlar yeni gönderimle hükümsüz kalır.
        var challenge = await Challenges
            .AsNoTracking()
            .Where(c => c.Channel == channel && c.Target == target && c.Purpose == purpose)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.CodeHash })
            .FirstOrDefaultAsync(cancellationToken);

        var now = clock.GetUtcNow();
        if (challenge is null || !await TryReserveAttemptAsync(challenge.Id, now, cancellationToken))
        {
            return false;
        }

        return OtpCode.Matches(challenge.CodeHash, code) && await TryConsumeAsync(challenge.Id, cancellationToken);
    }

    /// <summary>
    /// Karşılaştırmadan önce deneme hakkını atomik ayırır (koşullu UPDATE): eşzamanlı istekler toplamda
    /// MaxAttempts'ten fazla karşılaştırma yapamaz.
    /// </summary>
    private async Task<bool> TryReserveAttemptAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
        => await Challenges
            .Where(c => c.Id == id && c.ConsumedAt == null && c.ExpiresAt > now && c.Attempts < _options.MaxAttempts)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.Attempts, c => c.Attempts + 1), cancellationToken) == 1;

    /// <summary>Kodu tek seferlik tüketir; aynı kodla eşzamanlı ikinci istek 0 satır etkiler ve reddedilir.</summary>
    private async Task<bool> TryConsumeAsync(Guid id, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return await Challenges
            .Where(c => c.Id == id && c.ConsumedAt == null && c.ExpiresAt > now)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.ConsumedAt, now), cancellationToken) == 1;
    }
}
