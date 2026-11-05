using BaseForge.Core.Exceptions;

namespace Kantar.Custom.EDespatch.Providers;

/// <summary>DI'a kayıtlı tüm <see cref="IEDespatchProvider"/>'ları türlerine göre eşler.</summary>
internal sealed class EDespatchProviderRegistry : IEDespatchProviderRegistry
{
    private readonly Dictionary<EDespatchProviderKind, IEDespatchProvider> _providers;

    public EDespatchProviderRegistry(IEnumerable<IEDespatchProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Kind);
        Kinds = [.. _providers.Keys.Order()];
    }

    /// <inheritdoc />
    public IReadOnlyCollection<EDespatchProviderKind> Kinds { get; }

    /// <inheritdoc />
    public IEDespatchProvider Get(EDespatchProviderKind kind)
        => _providers.TryGetValue(kind, out var provider)
            ? provider
            : throw new ValidationException("ProviderKind", $"'{kind}' irsaliye yöntemi bu sürümde desteklenmiyor.");
}

/// <summary>Kâğıt irsaliye: elektronik gönderim yoktur, numara kâğıt koçanından gelir.</summary>
internal sealed class PaperOnlyProvider : IEDespatchProvider
{
    /// <inheritdoc />
    public EDespatchProviderKind Kind => EDespatchProviderKind.PaperOnly;

    /// <inheritdoc />
    public bool RequiresUbl => false;

    /// <inheritdoc />
    public Task<DespatchSendResult> SendAsync(DespatchDocument document, byte[] ublXml, EDespatchCredentials credentials, CancellationToken cancellationToken)
        => Task.FromResult(new DespatchSendResult(DespatchState.NotApplicable, "Kâğıt irsaliye; elektronik gönderim yapılmaz."));

    /// <inheritdoc />
    public Task<DespatchStatusResult> GetStatusAsync(Guid ettn, EDespatchCredentials credentials, CancellationToken cancellationToken)
        => Task.FromResult(new DespatchStatusResult(DespatchState.NotApplicable, "Kâğıt irsaliyenin elektronik durumu yoktur."));

    /// <inheritdoc />
    public Task<ConnectionTestResult> TestConnectionAsync(EDespatchCredentials credentials, CancellationToken cancellationToken)
        => Task.FromResult(new ConnectionTestResult(true, "Kâğıt irsaliye bağlantı gerektirmez."));
}

/// <summary>
/// Elle yükleme: UBL XML üretilir, kullanıcı indirip GİB e-İrsaliye portalına ya da kendi entegratörüne yükler.
/// Durumu kullanıcı işaretler; burada sorgulanacak uzak sistem yoktur.
/// </summary>
internal sealed class ManualXmlProvider : IEDespatchProvider
{
    /// <inheritdoc />
    public EDespatchProviderKind Kind => EDespatchProviderKind.ManualXml;

    /// <inheritdoc />
    public bool RequiresUbl => true;

    /// <inheritdoc />
    public Task<DespatchSendResult> SendAsync(DespatchDocument document, byte[] ublXml, EDespatchCredentials credentials, CancellationToken cancellationToken)
        => Task.FromResult(new DespatchSendResult(DespatchState.AwaitingManualUpload,
            "XML hazır; indirip GİB e-İrsaliye portalına ya da entegratörünüze yükleyin, sonra durumu işaretleyin."));

    /// <inheritdoc />
    public Task<DespatchStatusResult> GetStatusAsync(Guid ettn, EDespatchCredentials credentials, CancellationToken cancellationToken)
        => Task.FromResult(new DespatchStatusResult(DespatchState.AwaitingManualUpload, "Durum elle işaretlenir."));

    /// <inheritdoc />
    public Task<ConnectionTestResult> TestConnectionAsync(EDespatchCredentials credentials, CancellationToken cancellationToken)
        => Task.FromResult(new ConnectionTestResult(true, "Elle yükleme bağlantı gerektirmez."));
}
