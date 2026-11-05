using System.Text.RegularExpressions;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Custom.EDespatch.Providers;

namespace Kantar.Custom.EDespatch.Settings;

/// <summary>Ayar yanıtı; şifre asla dönmez, yalnız kayıtlı olup olmadığı.</summary>
public sealed record EDespatchSettingsDto(
    EDespatchProviderKind ProviderKind,
    EDespatchEnvironment Environment,
    string? UserName,
    bool PasswordSaved,
    string? SeriesPrefix,
    string? SenderAlias,
    DateTimeOffset? UpdatedAt,
    IReadOnlyCollection<EDespatchProviderKind> AvailableProviders);

/// <summary>Kiracının e-İrsaliye ayarı.</summary>
public sealed class GetEDespatchSettingsQuery : IQuery<EDespatchSettingsDto>;

/// <summary>Kiracının e-İrsaliye ayarını kaydeder.</summary>
public sealed class SaveEDespatchSettingsCommand : ICommand
{
    /// <summary>Yöntem.</summary>
    public EDespatchProviderKind ProviderKind { get; set; }

    /// <summary>Ortam (Test/Live).</summary>
    public EDespatchEnvironment Environment { get; set; }

    /// <summary>Entegratör kullanıcı adı.</summary>
    public string? UserName { get; set; }

    /// <summary>Yeni şifre; boş bırakılırsa kayıtlı şifre korunur.</summary>
    public string? Password { get; set; }

    /// <summary>Kayıtlı şifreyi sil.</summary>
    public bool ClearPassword { get; set; }

    /// <summary>Seri öneki (3 karakter, büyük harf/rakam).</summary>
    public string? SeriesPrefix { get; set; }

    /// <summary>Varsayılan gönderici etiketi.</summary>
    public string? SenderAlias { get; set; }
}

/// <summary>
/// Seçili (ya da kaydetmeden denenen) sağlayıcıyla bağlantı testi. Boş alanlar kayıtlı ayardan tamamlanır;
/// şifre verilmezse kayıtlı şifre kullanılır.
/// </summary>
public sealed class TestEDespatchConnectionCommand : ICommand<ConnectionTestResult>
{
    /// <summary>Denenecek yöntem (boşsa kayıtlı).</summary>
    public EDespatchProviderKind? ProviderKind { get; set; }

    /// <summary>Ortam (boşsa kayıtlı).</summary>
    public EDespatchEnvironment? Environment { get; set; }

    /// <summary>Kullanıcı adı (boşsa kayıtlı).</summary>
    public string? UserName { get; set; }

    /// <summary>Şifre (boşsa kayıtlı).</summary>
    public string? Password { get; set; }
}

internal sealed class GetEDespatchSettingsHandler(EDespatchSettingsStore store, IEDespatchProviderRegistry registry, ICurrentTenant currentTenant)
    : IQueryHandler<GetEDespatchSettingsQuery, EDespatchSettingsDto>
{
    public async Task<EDespatchSettingsDto> Handle(GetEDespatchSettingsQuery request, CancellationToken cancellationToken)
    {
        var s = await store.FindAsync(currentTenant.RequireTenantId(), cancellationToken);
        return s is null
            ? new EDespatchSettingsDto(EDespatchProviderKind.PaperOnly, EDespatchEnvironment.Test, null, false, null, null, null, registry.Kinds)
            : new EDespatchSettingsDto(s.ProviderKind, s.Environment, s.UserName, !string.IsNullOrEmpty(s.ProtectedPassword),
                s.SeriesPrefix, s.SenderAlias, s.UpdatedAt, registry.Kinds);
    }
}

internal sealed partial class SaveEDespatchSettingsHandler(
    EDespatchDbContext db, EDespatchSettingsStore store, EDespatchSecretProtector protector,
    IEDespatchProviderRegistry registry, ICurrentTenant currentTenant, ICurrentUser currentUser)
    : ICommandHandler<SaveEDespatchSettingsCommand>
{
    public async Task Handle(SaveEDespatchSettingsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = currentTenant.RequireTenantId();
        var provider = registry.Get(request.ProviderKind);
        var prefix = request.SeriesPrefix?.Trim().ToUpperInvariant();
        var userName = Blank(request.UserName);

        var setting = await store.FindAsync(tenantId, cancellationToken);
        var isNew = setting is null;
        setting ??= new EDespatchSetting { TenantId = tenantId };

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (provider.RequiresUbl && (prefix is null || !PrefixPattern().IsMatch(prefix)))
        {
            errors[nameof(request.SeriesPrefix)] = ["Seri öneki 3 karakter olmalı (büyük harf ya da rakam, ör. PKN)."];
        }

        var willHavePassword = !string.IsNullOrEmpty(request.Password)
            || (!request.ClearPassword && !string.IsNullOrEmpty(setting.ProtectedPassword));
        if (request.ProviderKind == EDespatchProviderKind.ELogo)
        {
            if (userName is null)
            {
                errors[nameof(request.UserName)] = ["e-Logo kullanıcı adı zorunlu."];
            }

            if (!willHavePassword)
            {
                errors[nameof(request.Password)] = ["e-Logo şifresi zorunlu."];
            }
        }

        var alias = Blank(request.SenderAlias);
        if (alias is not null && !alias.StartsWith("urn:mail:", StringComparison.OrdinalIgnoreCase))
        {
            errors[nameof(request.SenderAlias)] = ["Gönderici etiketi 'urn:mail:' ile başlamalı."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        setting.ProviderKind = request.ProviderKind;
        setting.Environment = request.Environment;
        setting.UserName = userName;
        setting.SeriesPrefix = string.IsNullOrEmpty(prefix) ? null : prefix;
        setting.SenderAlias = alias;
        if (request.ClearPassword)
        {
            setting.ProtectedPassword = null;
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            setting.ProtectedPassword = protector.Protect(request.Password);
        }

        setting.UpdatedAt = DateTimeOffset.UtcNow;
        setting.UpdatedBy = currentUser.UserId;
        if (isNew)
        {
            db.Settings.Add(setting);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex("^[A-Z0-9]{3}$")]
    private static partial Regex PrefixPattern();
}

internal sealed class TestEDespatchConnectionHandler(EDespatchSettingsStore store, IEDespatchProviderRegistry registry, ICurrentTenant currentTenant)
    : ICommandHandler<TestEDespatchConnectionCommand, ConnectionTestResult>
{
    public async Task<ConnectionTestResult> Handle(TestEDespatchConnectionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var saved = await store.FindAsync(currentTenant.RequireTenantId(), cancellationToken);
        var savedCredentials = saved is null ? EDespatchCredentials.None : store.CredentialsOf(saved);
        var kind = request.ProviderKind ?? saved?.ProviderKind ?? EDespatchProviderKind.PaperOnly;
        var credentials = savedCredentials with
        {
            Environment = request.Environment ?? savedCredentials.Environment,
            UserName = string.IsNullOrWhiteSpace(request.UserName) ? savedCredentials.UserName : request.UserName.Trim(),
            Password = string.IsNullOrEmpty(request.Password) ? savedCredentials.Password : request.Password,
        };
        return await registry.Get(kind).TestConnectionAsync(credentials, cancellationToken);
    }
}
