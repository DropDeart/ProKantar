using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Tracking.Common;
using Tracking.Data;
using Tracking.Data.Pings;
using Tracking.Entities;

namespace Tracking.Features.Trips;

/// <summary>Kiracının seferlerini sayfalı listeler (en yeni plan önce).</summary>
public sealed class GetTripsQuery : PagedRequest, IQuery<PagedResult<TripListItemDto>>
{
    /// <summary>Durum filtresi.</summary>
    public TripStatus? Status { get; set; }

    /// <summary>Şoför filtresi (platform/Driver).</summary>
    public Guid? DriverId { get; set; }

    /// <summary>Araç filtresi (platform/Vehicle).</summary>
    public Guid? VehicleId { get; set; }

    /// <summary>Plan tarihi alt sınırı.</summary>
    public DateTimeOffset? PlannedFrom { get; set; }

    /// <summary>Plan tarihi üst sınırı.</summary>
    public DateTimeOffset? PlannedTo { get; set; }
}

/// <summary>Kiracının tek seferi; yoksa null.</summary>
public sealed class GetTripByIdQuery : IQuery<TripDto?>
{
    /// <summary>Sefer (= sevkiyat) kimliği.</summary>
    public Guid Id { get; set; }
}

/// <summary>Seferin harita izi; sefer yoksa null.</summary>
public sealed class GetTripTrackQuery : IQuery<TripTrackDto?>
{
    /// <summary>Sefer kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class GetTripsHandler(TrackingDbContext db, ICurrentTenant currentTenant)
    : IQueryHandler<GetTripsQuery, PagedResult<TripListItemDto>>
{
    public async Task<PagedResult<TripListItemDto>> Handle(GetTripsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = currentTenant.RequireTenantId();

        var query = db.Trips.AsNoTracking().Where(t => t.TenantId == tenantId);
        if (request.Status is { } status) query = query.Where(t => t.Status == status);
        if (request.DriverId is { } driverId) query = query.Where(t => t.DriverId == driverId);
        if (request.VehicleId is { } vehicleId) query = query.Where(t => t.VehicleId == vehicleId);
        if (request.PlannedFrom is { } from) query = query.Where(t => t.PlannedAt >= from);
        if (request.PlannedTo is { } to) query = query.Where(t => t.PlannedAt <= to);
        if (LikePattern.ContainsOrNull(request.Search) is { } pattern)
        {
            query = query.Where(t =>
                EF.Functions.ILike(t.Plate, pattern, LikePattern.EscapeCharacter) ||
                EF.Functions.ILike(t.ShipmentNo, pattern, LikePattern.EscapeCharacter) ||
                (t.DriverName != null && EF.Functions.ILike(t.DriverName, pattern, LikePattern.EscapeCharacter)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.PlannedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(TripListItemDto.Projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<TripListItemDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }
}

internal sealed class GetTripByIdHandler(TrackingDbContext db, ICurrentTenant currentTenant)
    : IQueryHandler<GetTripByIdQuery, TripDto?>
{
    public async Task<TripDto?> Handle(GetTripByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = currentTenant.RequireTenantId();

        var trip = await db.Trips.AsNoTracking()
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.TenantId == tenantId, cancellationToken);
        return trip is null ? null : TripDto.From(trip);
    }
}

internal sealed class GetTripTrackHandler(
    TrackingDbContext db, ICurrentTenant currentTenant, ILocationPingStore pings, ITripRouteStore routes)
    : IQueryHandler<GetTripTrackQuery, TripTrackDto?>
{
    public async Task<TripTrackDto?> Handle(GetTripTrackQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = currentTenant.RequireTenantId();

        var trip = await db.Trips.AsNoTracking()
            .Where(t => t.Id == request.Id && t.TenantId == tenantId)
            .Select(t => new { t.Id, t.Status, Since = t.StartedAt ?? t.PlannedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (trip is null)
        {
            return null;
        }

        // Biten seferde kayıtlı rota kullanılır (ham konumlar 6 ay sonra silinir).
        var route = trip.Status == TripStatus.Completed
            ? await routes.GetGeoJsonAsync(trip.Id, cancellationToken)
            : await pings.GetLineGeoJsonAsync(trip.Id, trip.Since, cancellationToken);
        var last = await pings.GetLastAsync(trip.Id, trip.Since, cancellationToken);

        return new TripTrackDto(route, last);
    }
}
