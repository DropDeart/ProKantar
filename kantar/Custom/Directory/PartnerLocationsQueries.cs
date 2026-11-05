using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Directory;

/// <summary>Bağlı firmanın (ProKantar şirketi) teslimat noktası olarak seçilebilecek istasyonu.</summary>
public sealed record PartnerStationDto(
    Guid Id, string Name, string? Address, string? City, string? District, string? Phone, decimal Latitude, decimal Longitude, int RadiusM);

/// <summary>
/// Cari listedeki bağlı firmanın konumu tanımlı aktif istasyonları. Firmalar birbirinin tesis adresini zaten bilir;
/// yine de yalnız cari listesinde o şirketi bağlamış kiracı görür ve yalnız adres/konum döner.
/// </summary>
public sealed class PartnerStationsQuery : IQuery<IReadOnlyList<PartnerStationDto>>
{
    /// <summary>Cari firma (LinkedTenantId'si dolu olmalı).</summary>
    public Guid CompanyId { get; set; }
}

internal sealed class PartnerStationsHandler(KantarDbContext db) : IQueryHandler<PartnerStationsQuery, IReadOnlyList<PartnerStationDto>>
{
    public async Task<IReadOnlyList<PartnerStationDto>> Handle(PartnerStationsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Kiracı filtresi: firma çağıranın cari listesinde olmalı.
        var linkedTenantId = await db.Companies
            .Where(c => c.Id == request.CompanyId)
            .Select(c => new { c.LinkedTenantId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);
        if (linkedTenantId.LinkedTenantId is not { } partnerTenantId)
        {
            return [];
        }

        return await db.Stations.IgnoreQueryFilters()
            .Where(s => s.TenantId == partnerTenantId && !s.IsDeleted && s.IsActive && s.Latitude != null && s.Longitude != null)
            .OrderBy(s => s.Name)
            .Select(s => new PartnerStationDto(s.Id, s.Name, s.Address, s.City, s.District, s.Phone, s.Latitude!.Value, s.Longitude!.Value, s.RadiusM))
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Son sevkiyatlarda kullanılan teslimat noktası (hızlı seçim).</summary>
public sealed record RecentDeliveryLocationDto(
    Guid Id, string Name, Guid CompanyId, string CompanyName, string? City, string? District, string? Address, DateTimeOffset LastUsedAt, int UseCount);

/// <summary>Son 90 günde sevkiyatlarda en son kullanılan teslimat noktaları (en çok 10).</summary>
public sealed class RecentDeliveryLocationsQuery : IQuery<IReadOnlyList<RecentDeliveryLocationDto>>;

internal sealed class RecentDeliveryLocationsHandler(KantarDbContext db, ICurrentTenant currentTenant)
    : IQueryHandler<RecentDeliveryLocationsQuery, IReadOnlyList<RecentDeliveryLocationDto>>
{
    private const int MaxResults = 10;
    private static readonly TimeSpan Window = TimeSpan.FromDays(90);

    public async Task<IReadOnlyList<RecentDeliveryLocationDto>> Handle(RecentDeliveryLocationsQuery request, CancellationToken cancellationToken)
    {
        _ = currentTenant.RequireTenantId();
        var since = DateTimeOffset.UtcNow - Window;

        var usage = await db.ShipmentStops
            .Where(s => s.DeliveryLocationId != null && s.CreatedAt >= since
                && db.Shipments.Any(sh => sh.Id == s.ShipmentId && sh.Status != ShipmentStatus.Cancelled))
            .GroupBy(s => s.DeliveryLocationId!.Value)
            .Select(g => new { Id = g.Key, LastUsedAt = g.Max(s => s.CreatedAt), UseCount = g.Count() })
            .OrderByDescending(x => x.LastUsedAt)
            .Take(MaxResults)
            .ToListAsync(cancellationToken);

        var ids = usage.Select(u => u.Id).ToList();
        var locations = await db.DeliveryLocations
            .Where(l => ids.Contains(l.Id) && l.IsActive)
            .Select(l => new { l.Id, l.Name, l.CompanyId, CompanyName = l.Company!.ShortName ?? l.Company.Name, l.City, l.District, l.Address })
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        return usage
            .Where(u => locations.ContainsKey(u.Id))
            .Select(u =>
            {
                var l = locations[u.Id];
                return new RecentDeliveryLocationDto(l.Id, l.Name, l.CompanyId, l.CompanyName, l.City, l.District, l.Address, u.LastUsedAt, u.UseCount);
            })
            .ToList();
    }
}
