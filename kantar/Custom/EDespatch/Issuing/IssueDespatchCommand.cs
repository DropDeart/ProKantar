using System.Text;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using BaseForge.Core.Messaging;
using Kantar.Custom.EDespatch.Providers;
using Kantar.Custom.EDespatch.Settings;
using Kantar.Data;
using Kantar.Entities;
using Kantar.Features.Waybills;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>Tamamlanmış çıkış tartımına kiracının yöntemiyle irsaliye keser.</summary>
public sealed class IssueDespatchCommand : ICommand<WaybillView>
{
    /// <summary>Tartım (route'tan).</summary>
    public Guid WeighingId { get; set; }

    /// <summary>Şoför TCKN'si; platformda kayıtlı değilse zorunlu. Yalnız bu irsaliyeye yazılır.</summary>
    public string? DriverNationalId { get; set; }

    /// <summary>Sevkiyatsız tartımda teslim noktası (yoksa cari adresi).</summary>
    public Guid? DeliveryLocationId { get; set; }

    /// <summary>Belgeye düşülecek not.</summary>
    public string? Note { get; set; }
}

/// <summary>Kiracı ayarındaki seri öneki.</summary>
internal static class DespatchSeries
{
    /// <summary>Kâğıt irsaliyede önek girilmemişse kullanılan önek.</summary>
    public const string PaperDefault = "IRS";

    /// <summary>Etkin önek; e-İrsaliye yönteminde önek yoksa 400.</summary>
    public static string PrefixOf(EDespatchTenantConfig config, IEDespatchProvider provider)
        => config.SeriesPrefix
           ?? (provider.RequiresUbl
               ? throw new ValidationException("SeriesPrefix", "e-İrsaliye seri öneki tanımlı değil; ayarlardan girin.")
               : PaperDefault);
}

/// <summary>
/// Akış: ayar çözülür, taslak kurulup doğrulanır (uzak çağrılar kilit dışında), transaction'da tartım
/// <c>FOR UPDATE</c> kilitlenip uygunluk yeniden denetlenir, numara ayrılır, UBL üretilir, kayıt commit edilir.
/// Sağlayıcıya gönderim commit'ten sonradır; sonuç ayrı kayıtla yazılır.
/// </summary>
internal sealed class IssueDespatchHandler(
    KantarDbContext db, IUnitOfWork unitOfWork, IEventBus eventBus, ICurrentTenant currentTenant,
    EDespatchSettingsStore settings, IEDespatchProviderRegistry registry, IUblDespatchBuilder ublBuilder,
    DespatchDraftBuilder drafts, DespatchDelivery delivery, WaybillLinks links)
    : ICommandHandler<IssueDespatchCommand, WaybillView>
{
    public async Task<WaybillView> Handle(IssueDespatchCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = currentTenant.RequireTenantId();
        var config = await settings.ResolveAsync(tenantId, cancellationToken);
        var provider = registry.Get(config.ProviderKind);
        var prefix = DespatchSeries.PrefixOf(config, provider);
        var issuedAt = DateTimeOffset.UtcNow;

        var snapshot = await db.Weighings.AsNoTracking().FirstOrDefaultAsync(w => w.Id == request.WeighingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Weighing), request.WeighingId);

        // Numara yer tutucuyla doğrulanır; hatalı veride sayaç hiç kilitlenmez. Sevkiyatlı tartımda durak taslakta seçilir.
        var input = new DespatchDraftInput(tenantId, DespatchNumbers.Format(prefix, TurkeyTime.ToLocal(issuedAt).Year, 1),
            Guid.NewGuid(), issuedAt, request.DriverNationalId, request.DeliveryLocationId, request.Note);
        var draft = await drafts.BuildAsync(snapshot, input, cancellationToken);
        await EnsureEligibleAsync(snapshot, draft.ShipmentStopId, cancellationToken);
        DespatchValidation.EnsureValid(draft.Document);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Weighings\" WHERE \"Id\" = {request.WeighingId} FOR UPDATE", cancellationToken);
        var weighing = await db.Weighings.AsNoTracking().FirstAsync(w => w.Id == request.WeighingId, cancellationToken);
        await EnsureEligibleAsync(weighing, draft.ShipmentStopId, cancellationToken);

        var number = await DespatchNumbers.AllocateAsync(db, tenantId, prefix, issuedAt, cancellationToken);
        var document = draft.Document with { DocumentNo = number };
        var ubl = provider.RequiresUbl ? ublBuilder.Build(document) : null;

        var waybill = new Waybill
        {
            WaybillNo = number,
            WaybillType = provider.RequiresUbl ? WaybillWaybillType.EDespatch : WaybillWaybillType.Paper,
            IssuedAt = issuedAt,
            QuantityKg = draft.QuantityKg,
            DeliveryAddress = draft.DeliveryAddressText,
            EDespatchUuid = provider.RequiresUbl ? document.Ettn : null,
            EDespatchStatus = provider.RequiresUbl ? WaybillEDespatchStatus.Draft : null,
            EDespatchProviderKind = WaybillStates.ToEntity(config.ProviderKind),
            UblXml = ubl is null ? null : Encoding.UTF8.GetString(ubl),
            DriverName = draft.DriverName,
            DriverNationalId = document.Drivers.FirstOrDefault()?.NationalId,
            Plate = DespatchRules.NormalizePlate(document.Plate),
            TrailerPlate = document.TrailerPlate is null ? null : DespatchRules.NormalizePlate(document.TrailerPlate),
            WeighingId = weighing.Id,
            CompanyId = draft.CompanyId,
            DeliveryLocationId = draft.DeliveryLocationId,
        };
        db.Waybills.Add(waybill);

        if (draft.ShipmentStopId is { } stopId)
        {
            var stop = await db.ShipmentStops.FirstAsync(s => s.Id == stopId, cancellationToken);
            stop.WaybillId = waybill.Id;
        }

        await eventBus.PublishAsync(new WaybillCreatedEvent { Data = WaybillDto.From(waybill) }, cancellationToken);

        // Şoföre bildirim zinciri platformda (SignalR → push → SMS); link giriş istemez.
        var shipmentNo = weighing.ShipmentId is { } shipmentId
            ? await db.Shipments.Where(s => s.Id == shipmentId).Select(s => s.ShipmentNo).FirstOrDefaultAsync(cancellationToken)
            : null;
        await eventBus.PublishAsync(new WaybillIssuedEvent
        {
            TenantId = tenantId,
            WaybillId = waybill.Id,
            WaybillNo = waybill.WaybillNo,
            WeighingId = weighing.Id,
            ShipmentId = weighing.ShipmentId,
            ShipmentNo = shipmentNo,
            DriverId = weighing.DriverId,
            DriverName = waybill.DriverName,
            Plate = waybill.Plate ?? weighing.Plate,
            DeliveryAddress = waybill.DeliveryAddress,
            LinkUrl = await links.UrlForAsync(waybill.Id, issuedAt, cancellationToken),
            IssuedAt = issuedAt,
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        await delivery.SendAsync(waybill, document, ubl ?? [], config.Credentials);
        return await WaybillViews.GetAsync(db, waybill.Id, CancellationToken.None);
    }

    private async Task EnsureEligibleAsync(Weighing weighing, Guid? stopId, CancellationToken ct)
    {
        var hasValid = await DespatchEligibility.HasValidWaybillAsync(db, weighing.Id, stopId, ct);
        DespatchValidation.ThrowIfAny(DespatchEligibility.Check(weighing, hasValid));
    }
}
