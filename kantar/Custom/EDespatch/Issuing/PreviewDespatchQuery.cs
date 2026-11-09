using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Custom.EDespatch.Providers;
using Kantar.Custom.EDespatch.Settings;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>Kesmeden önce ön kontrol: yöntem, eksik alanlar ve belgenin özeti (numara ayrılmaz, kayıt yazılmaz).</summary>
public sealed class PreviewDespatchQuery : IQuery<DespatchPreview>
{
    /// <summary>Tartım (route'tan).</summary>
    public Guid WeighingId { get; set; }

    /// <summary>Sevkiyatsız tartımda denenecek teslim noktası.</summary>
    public Guid? DeliveryLocationId { get; set; }
}

/// <summary>Ön kontrol yanıtı.</summary>
/// <param name="ProviderKind">Kiracının yöntemi.</param>
/// <param name="SeriesPrefix">Kullanılacak seri öneki.</param>
/// <param name="Errors">Alan bazında eksikler (POST'un 400 anahtarlarıyla aynı).</param>
/// <param name="DriverNationalIdRequired">Platformda TCKN yok; operatör girmeli.</param>
/// <param name="Document">Belge özeti.</param>
/// <param name="Warnings">Kesmeyi engellemeyen uyarılar (net ile plan toplamı farkı vb.).</param>
/// <param name="ShipmentStopId">Sevkiyatlı tartımda irsaliyenin kesileceği teslimat durağı.</param>
public sealed record DespatchPreview(
    EDespatchProviderKind ProviderKind, string? SeriesPrefix, IReadOnlyList<DespatchRuleError> Errors,
    bool DriverNationalIdRequired, DespatchPreviewDocument Document, IReadOnlyList<string> Warnings, Guid? ShipmentStopId);

/// <summary>Belge özeti.</summary>
/// <param name="CustomerName">Alıcı unvanı.</param>
/// <param name="DeliveryAddress">Teslim adresi (tek satır).</param>
/// <param name="Plate">Araç plakası.</param>
/// <param name="TrailerPlate">Dorse plakası.</param>
/// <param name="DriverName">Şoför adı soyadı.</param>
/// <param name="Lines">Satırlar.</param>
public sealed record DespatchPreviewDocument(
    string? CustomerName, string DeliveryAddress, string Plate, string? TrailerPlate, string? DriverName, IReadOnlyList<DespatchPreviewLine> Lines);

/// <summary>Belge satırı.</summary>
/// <param name="Name">Malzeme adı.</param>
/// <param name="Code">Malzeme kodu.</param>
/// <param name="Quantity">Miktar.</param>
/// <param name="UnitCode">UBL birim kodu (KGM).</param>
public sealed record DespatchPreviewLine(string Name, string? Code, decimal Quantity, string UnitCode);

internal sealed class PreviewDespatchHandler(
    KantarDbContext db, ICurrentTenant currentTenant, EDespatchSettingsStore settings, IEDespatchProviderRegistry registry,
    DespatchDraftBuilder drafts)
    : IQueryHandler<PreviewDespatchQuery, DespatchPreview>
{
    public async Task<DespatchPreview> Handle(PreviewDespatchQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = currentTenant.RequireTenantId();
        var config = await settings.ResolveAsync(tenantId, cancellationToken);
        var provider = registry.Get(config.ProviderKind);
        var errors = new List<DespatchRuleError>();
        var prefix = config.SeriesPrefix ?? (provider.RequiresUbl ? null : DespatchSeries.PaperDefault);
        if (prefix is null)
        {
            errors.Add(new("SeriesPrefix", "e-İrsaliye seri öneki tanımlı değil; ayarlardan girin."));
        }

        var weighing = await db.Weighings.AsNoTracking().FirstOrDefaultAsync(w => w.Id == request.WeighingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Weighing), request.WeighingId);
        var issuedAt = DateTimeOffset.UtcNow;
        var draft = await drafts.BuildAsync(weighing, new DespatchDraftInput(
            tenantId, DespatchNumbers.Format(prefix ?? "AAA", TurkeyTime.ToLocal(issuedAt).Year, 1), Guid.NewGuid(), issuedAt,
            null, request.DeliveryLocationId, null), cancellationToken);
        var hasValid = await DespatchEligibility.HasValidWaybillAsync(db, weighing.Id, draft.ShipmentStopId, cancellationToken);
        errors.AddRange(DespatchEligibility.Check(weighing, hasValid));
        errors.AddRange(DespatchValidation.Check(draft.Document));

        var document = draft.Document;
        return new DespatchPreview(config.ProviderKind, prefix, errors, draft.DriverNationalIdRequired,
            new DespatchPreviewDocument(
                string.IsNullOrWhiteSpace(document.Customer.Name) ? null : document.Customer.Name,
                draft.DeliveryAddressText,
                DespatchRules.NormalizePlate(document.Plate),
                document.TrailerPlate is null ? null : DespatchRules.NormalizePlate(document.TrailerPlate),
                draft.DriverName,
                document.Lines.Select(l => new DespatchPreviewLine(l.Name, l.Code, l.Quantity, DespatchUnitCodes.Of(l.Unit))).ToList()),
            draft.Warnings, draft.ShipmentStopId);
    }
}
