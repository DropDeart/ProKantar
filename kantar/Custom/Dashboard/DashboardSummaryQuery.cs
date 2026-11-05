using BaseForge.Core.CQRS;
using Kantar.Data;
using Kantar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Dashboard;

/// <summary>Panel ana sayfası özeti: bugünkü tartımlar, sahadaki araçlar, aktif sevkiyatlar ve son 7 günün net tonajı.</summary>
public sealed class DashboardSummaryQuery : IQuery<DashboardSummaryDto>;

/// <summary>Panel özeti.</summary>
/// <param name="TodayWeighings">Bugün ilk tartımı yapılan (iptal edilmemiş) tartım sayısı.</param>
/// <param name="TodayNetKg">Bugün tamamlanan tartımların net toplamı.</param>
/// <param name="OpenWeighings">İkinci tartımı bekleyen (sahadaki) araç sayısı.</param>
/// <param name="ActiveShipments">Yola çıkmış ya da yolda olan sevkiyat sayısı.</param>
/// <param name="Daily">Son 7 gün (bugün dahil, eskiden yeniye) günlük net tonaj.</param>
public sealed record DashboardSummaryDto(
    int TodayWeighings,
    decimal TodayNetKg,
    int OpenWeighings,
    int ActiveShipments,
    IReadOnlyList<DailyNetDto> Daily);

/// <summary>Bir günün tamamlanan tartım sayısı ve net toplamı.</summary>
public sealed record DailyNetDto(DateOnly Date, int Count, decimal NetKg);

internal sealed class DashboardSummaryHandler(KantarDbContext db)
    : IQueryHandler<DashboardSummaryQuery, DashboardSummaryDto>
{
    private const int Days = 7;

    // Gün sınırı Türkiye saatine göre; kiracı bazlı saat dilimi gelince buradan okunur.
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public async Task<DashboardSummaryDto> Handle(DashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone).DateTime);
        var todayStart = StartOf(today);
        var rangeStart = StartOf(today.AddDays(1 - Days));

        var todayWeighings = await db.Weighings
            .CountAsync(w => w.FirstWeighedAt >= todayStart && w.Status != WeighingStatus.Cancelled, cancellationToken);
        var openWeighings = await db.Weighings.CountAsync(w => w.Status == WeighingStatus.Open, cancellationToken);
        var activeShipments = await db.Shipments.CountAsync(
            s => s.Status == ShipmentStatus.Dispatched || s.Status == ShipmentStatus.InProgress, cancellationToken);

        var completed = await db.Weighings.AsNoTracking()
            .Where(w => w.Status == WeighingStatus.Completed && w.SecondWeighedAt >= rangeStart)
            .Select(w => new { w.SecondWeighedAt, w.NetKg })
            .ToListAsync(cancellationToken);

        var byDay = completed
            .GroupBy(w => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(w.SecondWeighedAt!.Value, Zone).DateTime))
            .ToDictionary(g => g.Key, g => (Count: g.Count(), NetKg: g.Sum(w => w.NetKg ?? 0)));

        var daily = Enumerable.Range(0, Days)
            .Select(i => today.AddDays(i + 1 - Days))
            .Select(d => byDay.TryGetValue(d, out var v) ? new DailyNetDto(d, v.Count, v.NetKg) : new DailyNetDto(d, 0, 0))
            .ToList();

        return new DashboardSummaryDto(todayWeighings, daily[^1].NetKg, openWeighings, activeShipments, daily);
    }

    private static DateTimeOffset StartOf(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
