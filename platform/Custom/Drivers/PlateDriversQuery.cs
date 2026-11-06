using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Platform.Custom.Validation;
using Platform.Data;

namespace Platform.Custom.Drivers;

/// <summary>
/// Plakaya göre sevkiyat için önerilen şoförler: aracın firmanın filosundaki varsayılan şoförü (varsa ilk sırada) ve aracı
/// uygulamada aktif kullanan şoförler.
/// </summary>
public sealed class PlateDriversQuery : IQuery<PlateDriversDto>
{
    /// <summary>Plaka (biçim serbest).</summary>
    public string Plate { get; set; } = string.Empty;

    /// <summary>Çağıranın firması (controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? TenantId { get; set; }
}

/// <summary>Plakanın önerilen şoförleri.</summary>
/// <param name="InFleet">Araç çağıranın filosunda mı.</param>
/// <param name="DefaultDriverId">Filodaki varsayılan şoför (listede ilk sıradadır).</param>
/// <param name="Drivers">Varsayılan şoför + aracı aktif kullananlar, tekrarsız.</param>
public sealed record PlateDriversDto(bool InFleet, Guid? DefaultDriverId, IReadOnlyList<DriverSearchItemDto> Drivers);

internal sealed class PlateDriversHandler(PlatformDbContext db) : IQueryHandler<PlateDriversQuery, PlateDriversDto>
{
    private const int MaxResults = 10;

    public async Task<PlateDriversDto> Handle(PlateDriversQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plate = Plates.Key(request.Plate);
        if (!Plates.IsValid(plate))
        {
            throw new ValidationException(nameof(request.Plate), "Geçerli bir plaka girin.");
        }

        var vehicle = await db.Vehicles.AsNoTracking()
            .Where(v => v.Plate == plate)
            .Select(v => new
            {
                v.Id,
                Fleet = db.VehicleSpecs.Where(s => s.VehicleId == v.Id).Select(s => new { s.FleetTenantId, s.DefaultDriverId }).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (vehicle is null)
        {
            return new PlateDriversDto(false, null, []);
        }

        var inFleet = request.TenantId is not null && vehicle.Fleet?.FleetTenantId == request.TenantId;
        var defaultDriverId = inFleet ? vehicle.Fleet!.DefaultDriverId : null;
        var activeIds = db.DriverVehicles.Where(dv => dv.VehicleId == vehicle.Id && dv.EndedAt == null).Select(dv => dv.DriverId);
        var drivers = db.Drivers.AsNoTracking()
            .Where(d => d.IsActive && (d.Id == defaultDriverId || activeIds.Contains(d.Id)))
            .OrderBy(d => d.Id == defaultDriverId ? 0 : 1).ThenBy(d => d.FullName)
            .Take(MaxResults);
        var items = await DriverItems.ProjectAsync(db, drivers, false, cancellationToken);
        return new PlateDriversDto(inFleet, items.Any(d => d.Id == defaultDriverId) ? defaultDriverId : null, items);
    }
}
