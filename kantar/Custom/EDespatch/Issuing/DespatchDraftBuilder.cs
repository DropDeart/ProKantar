using System.Globalization;
using BaseForge.Core.Exceptions;
using Kantar.Custom.Shipments;
using Kantar.Custom.EDespatch.Parties;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.EDespatch.Issuing;

/// <summary>Taslak girdisi.</summary>
/// <param name="TenantId">İsteğin kiracısı.</param>
/// <param name="DocumentNo">Belge numarası (ön izlemede yer tutucu).</param>
/// <param name="Ettn">ETTN.</param>
/// <param name="IssuedAt">Düzenleme ve sevk anı.</param>
/// <param name="DriverNationalId">Operatörün girdiği TCKN (platformda yoksa kullanılır).</param>
/// <param name="DeliveryLocationId">Sevkiyatsız tartımda teslim noktası; çok duraklı sevkiyatta durak seçimi.</param>
/// <param name="Note">Serbest not.</param>
public sealed record DespatchDraftInput(
    Guid TenantId, string DocumentNo, Guid Ettn, DateTimeOffset IssuedAt, string? DriverNationalId, Guid? DeliveryLocationId, string? Note);

/// <summary>Tartımdan kurulan belge ve kayda girecek bağlantılar.</summary>
/// <param name="Document">e-İrsaliye belgesi.</param>
/// <param name="CompanyId">Alıcı cari.</param>
/// <param name="DeliveryLocationId">Teslim noktası (cari adresi kullanıldıysa null).</param>
/// <param name="ShipmentStopId">Bağlanacak sevkiyat durağı.</param>
/// <param name="DriverNationalIdRequired">Platformda TCKN yok; operatör girmeli.</param>
/// <param name="DeliveryAddressText">Teslim adresinin tek satırlık metni.</param>
/// <param name="DriverName">Şoför adı soyadı.</param>
/// <param name="QuantityKg">İrsaliyenin kg karşılığı (tek duraklıda net; çok duraklıda durağın planı; çevrilemezse null).</param>
/// <param name="Warnings">Kesmeyi engellemeyen uyarılar (ör. net ile plan toplamı arasında %2'den fazla fark).</param>
public sealed record DespatchDraft(
    DespatchDocument Document, Guid? CompanyId, Guid? DeliveryLocationId, Guid? ShipmentStopId,
    bool DriverNationalIdRequired, string DeliveryAddressText, string? DriverName, decimal? QuantityKg, IReadOnlyList<string> Warnings);

/// <summary>
/// Tamamlanmış tartımdan <see cref="DespatchDocument"/> kurar. Teslim adresi sırasıyla sevkiyatın teslim durağından,
/// gövdedeki teslim noktasından ya da cari firmanın adresinden gelir. Sevkiyata bağlı tartımda her teslimat durağına ayrı
/// irsaliye kesilir; satırlar durağın yük satırlarıdır: tek duraklı, tek satırlı ve ağırlık birimli yükte miktar tartılan
/// nettir, aksi halde planlanan miktar yazılır. Sevkiyatsız tartımda satır malzeme adı + kodu ve net kg'dır.
/// </summary>
public sealed class DespatchDraftBuilder(KantarDbContext db, ISupplierDirectory suppliers, IDriverDirectory drivers)
{
    private static readonly DespatchAddress EmptyAddress = new();

    /// <summary>Net ile plan toplamı arasında uyarı verilen sapma oranı.</summary>
    public const decimal DeviationThreshold = 0.02m;

    /// <summary>Taslağı kurar (doğrulamaz).</summary>
    public async Task<DespatchDraft> BuildAsync(Weighing weighing, DespatchDraftInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(weighing);
        ArgumentNullException.ThrowIfNull(input);

        var (location, stopId) = await ResolveDeliveryAsync(weighing, input.DeliveryLocationId, ct);
        var company = await FindCompanyAsync(weighing.CompanyId ?? location?.CompanyId, input.TenantId, ct);
        var deliveryAddress = location is not null
            ? new DespatchAddress { Street = location.Address, District = location.District, City = location.City, PostalCode = location.PostalCode }
            : company is not null ? AddressOf(company) : EmptyAddress;

        var (driver, nationalIdRequired) = await ResolveDriverAsync(weighing, input.DriverNationalId, ct);
        var load = await LoadOfAsync(weighing, stopId, input.TenantId, ct);
        var document = new DespatchDocument
        {
            Ettn = input.Ettn,
            DocumentNo = input.DocumentNo,
            IssuedAt = input.IssuedAt,
            DespatchedAt = input.IssuedAt,
            Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim(),
            Supplier = await suppliers.GetAsync(input.TenantId, ct) ?? new DespatchParty { TaxId = string.Empty, Name = string.Empty, Address = EmptyAddress },
            Station = await FindStationAsync(weighing.StationId, input.TenantId, ct),
            Customer = company is null
                ? new DespatchParty { TaxId = string.Empty, Name = string.Empty, Address = EmptyAddress }
                : new DespatchParty
                {
                    TaxId = company.TaxNo ?? string.Empty,
                    Name = company.Name,
                    TaxOffice = company.TaxOffice,
                    Phone = company.Phone,
                    Email = company.Email,
                    Address = AddressOf(company),
                },
            DeliveryAddress = deliveryAddress,
            Plate = weighing.Plate,
            TrailerPlate = string.IsNullOrWhiteSpace(weighing.TrailerPlate) ? null : weighing.TrailerPlate,
            Drivers = [driver],
            Lines = load.Lines,
            WeighingTicketNo = weighing.TicketNo,
        };

        var driverName = string.Join(' ', new[] { driver.FirstName, driver.FamilyName }.Where(s => s.Length > 0));
        return new DespatchDraft(document, company?.Id, location?.Id, stopId, nationalIdRequired,
            OneLine(deliveryAddress), driverName.Length == 0 ? null : driverName, load.QuantityKg, load.Warnings);
    }

    /// <summary>Adresi tek satıra çevirir (ör. "Atatürk Cd. 5, Kadıköy/İstanbul 34710").</summary>
    public static string OneLine(DespatchAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        var place = string.Join('/', new[] { address.District, address.City }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));
        return string.Join(", ", new[] { address.Street?.Trim(), string.Join(' ', new[] { place, address.PostalCode?.Trim() }.Where(s => !string.IsNullOrEmpty(s))) }
            .Where(s => !string.IsNullOrEmpty(s)));
    }

    private async Task<(DeliveryLocation? Location, Guid? StopId)> ResolveDeliveryAsync(Weighing weighing, Guid? requested, CancellationToken ct)
    {
        if (weighing.ShipmentId is { } shipmentId)
        {
            var stops = await db.ShipmentStops.AsNoTracking()
                .Where(s => s.ShipmentId == shipmentId && s.StopType == ShipmentStopStopType.Delivery && s.DeliveryLocationId != null)
                .OrderBy(s => s.Sequence)
                .Select(s => new { s.Id, s.DeliveryLocationId, s.WaybillId })
                .ToListAsync(ct);
            var valid = await db.Waybills.AsNoTracking().Where(w => w.WeighingId == weighing.Id).Where(WaybillStates.IsValid)
                .Select(w => w.Id).ToListAsync(ct);

            // İstenen durak; yoksa geçerli irsaliyesi olmayan ilk durak.
            var stop = stops.FirstOrDefault(s => s.DeliveryLocationId == requested)
                ?? stops.FirstOrDefault(s => s.WaybillId is null || !valid.Contains(s.WaybillId.Value))
                ?? stops.FirstOrDefault();
            if (stop is not null)
            {
                var location = await db.DeliveryLocations.IgnoreQueryFilters().AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == stop.DeliveryLocationId && l.TenantId == weighing.TenantId, ct);
                return (location, stop.Id);
            }
        }

        if (requested is { } locationId)
        {
            var location = await db.DeliveryLocations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == locationId, ct)
                ?? throw new NotFoundException(nameof(DeliveryLocation), locationId);
            return (location, null);
        }

        return (null, null);
    }

    private async Task<(DespatchDriver Driver, bool NationalIdRequired)> ResolveDriverAsync(Weighing weighing, string? entered, CancellationToken ct)
    {
        var record = weighing.DriverId is { } driverId ? await drivers.GetAsync(driverId, ct) : null;
        var name = !string.IsNullOrWhiteSpace(weighing.DriverName) ? weighing.DriverName : record?.FullName;
        var parts = (name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var (first, family) = parts.Length switch
        {
            0 => (string.Empty, string.Empty),
            1 => (parts[0], string.Empty),
            _ => (string.Join(' ', parts[..^1]), parts[^1]),
        };

        var required = record?.NationalId is null;
        var nationalId = record?.NationalId ?? entered?.Trim() ?? string.Empty;
        return (new DespatchDriver(first, family, nationalId), required);
    }

    private Task<Company?> FindCompanyAsync(Guid? companyId, Guid tenantId, CancellationToken ct)
        => companyId is { } id
            ? db.Companies.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct)
            : Task.FromResult<Company?>(null);

    /// <summary>İstasyon isteğe bağlı fiziki konumdur; il/ilçesi girilmemişse belgeye konmaz.</summary>
    private async Task<DespatchSite?> FindStationAsync(Guid stationId, Guid tenantId, CancellationToken ct)
    {
        var station = await db.Stations.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stationId && s.TenantId == tenantId, ct);
        return station is null || string.IsNullOrWhiteSpace(station.City) || string.IsNullOrWhiteSpace(station.District)
            ? null
            : new DespatchSite(station.Name, new DespatchAddress
            {
                Street = station.Address, District = station.District, City = station.City, PostalCode = station.PostalCode,
            });
    }

    /// <summary>
    /// Kesilmiş irsaliyenin satırları (giriş istemeyen sayfa için): durağı varsa durağın yükü, yoksa tartımın malzemesi.
    /// Yayınlanmış plan ve tamamlanmış tartım değişmediği için kesim anındaki satırlarla aynıdır. Kiracı açıkça verilir.
    /// </summary>
    public async Task<IReadOnlyList<DespatchLine>> LinesOfWaybillAsync(Guid waybillId, Weighing weighing, Guid tenantId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(weighing);
        var stopId = await db.ShipmentStops.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.WaybillId == waybillId && s.TenantId == tenantId && !s.IsDeleted)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);
        return (await LoadOfAsync(weighing, stopId, tenantId, ct)).Lines;
    }

    private sealed record Load(IReadOnlyList<DespatchLine> Lines, decimal? QuantityKg, IReadOnlyList<string> Warnings);

    /// <summary>İrsaliye satırları: sevkiyat durağının yükü; durak ya da yük yoksa tartımın malzemesi ve neti.</summary>
    private async Task<Load> LoadOfAsync(Weighing weighing, Guid? stopId, Guid tenantId, CancellationToken ct)
    {
        if (weighing.ShipmentId is not { } shipmentId || stopId is null)
        {
            return new Load([await LineOfAsync(weighing, tenantId, ct)], weighing.NetKg, []);
        }

        var all = await db.ShipmentStopLines.IgnoreQueryFilters().AsNoTracking()
            .Where(l => !l.IsDeleted && l.TenantId == tenantId && l.ShipmentStop!.ShipmentId == shipmentId && l.ShipmentStop.StopType == ShipmentStopStopType.Delivery)
            .OrderBy(l => l.ShipmentStop!.Sequence).ThenBy(l => l.Sequence)
            .Select(l => new { Line = l, l.Material })
            .ToListAsync(ct);
        var mine = all.Where(x => x.Line.ShipmentStopId == stopId).ToList();
        if (mine.Count == 0)
        {
            return new Load([await LineOfAsync(weighing, tenantId, ct)], weighing.NetKg, []);
        }

        var net = weighing.NetKg ?? 0m;
        var warnings = new List<string>();
        var plannedKg = all.Select(x => x.Material is null ? null : ShipmentLoadCapacity.KgOf(x.Line, x.Material)).ToList();
        var total = plannedKg.Sum() ?? 0m;
        if (net > 0 && total > 0 && plannedKg.All(k => k is not null))
        {
            var deviation = Math.Abs(net - total) / total;
            if (deviation > DeviationThreshold)
            {
                var tr = CultureInfo.GetCultureInfo("tr-TR");
                warnings.Add($"Tartılan net {net.ToString("N0", tr)} kg, plan toplamı {total.ToString("N0", tr)} kg (%{(deviation * 100).ToString("N1", tr)} fark).");
            }
        }

        var singleStop = all.Select(x => x.Line.ShipmentStopId).Distinct().Count() == 1;
        if (singleStop && mine.Count == 1 && mine[0].Line.Unit is MaterialUnit.Kg or MaterialUnit.Ton && net > 0)
        {
            var line = mine[0].Line;
            var quantity = line.Unit == MaterialUnit.Ton ? net / 1000m : net;
            return new Load([new DespatchLine(line.MaterialName, NullIfEmpty(line.MaterialCode), quantity, UnitOf(line.Unit))], net, warnings);
        }

        var lines = mine.Select(x => new DespatchLine(x.Line.MaterialName, NullIfEmpty(x.Line.MaterialCode), x.Line.PlannedQuantity, UnitOf(x.Line.Unit))).ToList();
        var stopKg = mine.Select(x => x.Material is null ? null : ShipmentLoadCapacity.KgOf(x.Line, x.Material)).ToList();
        decimal? quantityKg = singleStop ? net : stopKg.All(k => k is not null) ? stopKg.Sum() : null;
        return new Load(lines, quantityKg, warnings);
    }

    private static DespatchUnit UnitOf(MaterialUnit unit) => unit switch
    {
        MaterialUnit.Ton => DespatchUnit.Ton,
        MaterialUnit.M3 => DespatchUnit.M3,
        MaterialUnit.Piece => DespatchUnit.Piece,
        _ => DespatchUnit.Kg,
    };

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private async Task<DespatchLine> LineOfAsync(Weighing weighing, Guid tenantId, CancellationToken ct)
    {
        var material = await db.Materials.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.Id == weighing.MaterialId && m.TenantId == tenantId)
            .Select(m => new { m.Name, m.Code })
            .FirstOrDefaultAsync(ct);
        return new DespatchLine(material?.Name ?? string.Empty, material?.Code, weighing.NetKg ?? 0, DespatchUnit.Kg);
    }

    private static DespatchAddress AddressOf(Company company)
        => new() { Street = company.Address, District = company.District, City = company.City, PostalCode = company.PostalCode };
}
