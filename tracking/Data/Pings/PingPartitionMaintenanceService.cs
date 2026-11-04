using Microsoft.Extensions.Options;
using Tracking.Configuration;

namespace Tracking.Data.Pings;

/// <summary>Açılışta ve günde bir: konum tablosunu/bölümlerini hazırlar, süresi dolanları siler.</summary>
internal sealed class PingPartitionMaintenanceService(
    IServiceScopeFactory scopeFactory,
    IOptions<TrackingOptions> options,
    TimeProvider timeProvider,
    ILogger<PingPartitionMaintenanceService> logger) : BackgroundService
{
    private const int MonthsAhead = 2;
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = Interval;
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Postgres henüz ayakta değilse kısa aralıkla yeniden dene.
                logger.LogWarning(ex, "Konum bölümleri hazırlanamadı; {Delay} sonra yeniden denenecek.", RetryDelay);
                delay = RetryDelay;
            }

            await Task.Delay(delay, timeProvider, stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IPingPartitionManager>();
        var now = timeProvider.GetUtcNow();

        await manager.EnsureTableAsync(cancellationToken);
        await manager.EnsurePartitionsAsync(now, MonthsAhead, cancellationToken);

        var dropped = await manager.DropExpiredAsync(now, options.Value.RetentionMonths, cancellationToken);
        if (dropped.Count > 0)
        {
            logger.LogInformation("Saklama süresi dolan konum bölümleri silindi: {Partitions}", string.Join(", ", dropped));
        }
    }
}
