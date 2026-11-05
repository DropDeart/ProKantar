using Identity.Configuration;
using Identity.Data;
using Identity.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Identity.Tenancy;

/// <summary>Şirket kararını (onay/ret/askı) başvuru sahibine e-postayla bildirir.</summary>
public interface ITenantReviewNotifier
{
    /// <summary>Gönderim hatası kararı geri almaz; log'a yazılır.</summary>
    Task NotifyAsync(Tenant tenant, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ITenantReviewNotifier" />
internal sealed partial class TenantReviewNotifier(
    IdentityServiceDbContext db,
    IEmailSender email,
    AuthOptions options,
    ILogger<TenantReviewNotifier> logger) : ITenantReviewNotifier
{
    public async Task NotifyAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var applicant = await db.Users.AsNoTracking()
            .Where(u => u.Id == tenant.CreatedByUserId)
            .Select(u => new { u.Email, u.FullName })
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrEmpty(applicant?.Email))
        {
            return;
        }

        var reason = tenant.ReviewNote?.TrimEnd('.', ' ');
        var (subject, title, text, button) = tenant.Status switch
        {
            TenantStatus.Active => ("Şirketiniz onaylandı", "Başvurunuz onaylandı",
                $"{tenant.Name} için yaptığınız başvuru onaylandı. Giriş yaparak şirketinizi kullanmaya başlayabilirsiniz.", "Panele git"),
            TenantStatus.Rejected => ("Başvurunuz hakkında", "Başvurunuz reddedildi",
                $"{tenant.Name} için yaptığınız başvuru reddedildi. Gerekçe: {reason}. Bilgileri ya da belgeleri düzeltip yeniden gönderebilirsiniz.",
                "Başvuruyu düzenle"),
            TenantStatus.Suspended => ("Şirketiniz askıya alındı", "Şirketiniz askıya alındı",
                $"{tenant.Name} erişime kapatıldı.{(reason is null ? string.Empty : $" Gerekçe: {reason}.")}", "Ayrıntılar"),
            _ => (null, null, null, null),
        };
        if (subject is null)
        {
            return;
        }

        try
        {
            var greeting = string.IsNullOrWhiteSpace(applicant.FullName) ? "Merhaba," : $"Merhaba {applicant.FullName},";
            var link = $"{options.MultiTenancy.PortalUrl.TrimEnd('/')}/sirket/{tenant.Id}";
            await email.SendAsync(EmailTemplate.Build(applicant.Email, subject, options.Messaging.Smtp.FromName, title!,
                [greeting, text!], buttonText: button, buttonUrl: link), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogSendFailed(logger, tenant.Id, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Şirket kararı e-postası gönderilemedi: {TenantId}")]
    private static partial void LogSendFailed(ILogger logger, Guid tenantId, Exception exception);
}
