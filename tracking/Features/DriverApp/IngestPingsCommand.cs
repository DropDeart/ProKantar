using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tracking.Common;
using Tracking.Configuration;
using Tracking.Data;
using Tracking.Data.Pings;
using Tracking.Domain;
using Tracking.Entities;

namespace Tracking.Features.DriverApp;

/// <summary>Şoför uygulamasından toplu konum (15 sn'de bir, çevrimdışı birikenler dahil).</summary>
public sealed class IngestPingsCommand : ICommand<IngestPingsResult>
{
    /// <summary>Konumlar (sıra önemsiz; zamana göre işlenir).</summary>
    public List<PingInput> Pings { get; set; } = [];
}

/// <summary>Tek konum.</summary>
public sealed record PingInput(
    double Latitude,
    double Longitude,
    double? AccuracyM,
    double? SpeedMps,
    double? Heading,
    DateTimeOffset RecordedAt);

/// <summary>Sonuç: açık sefer yoksa konum saklanmaz (Stored = 0).</summary>
public sealed record IngestPingsResult(int Stored, Guid? TripId, TripStatus? TripStatus);

/// <summary>
/// Konumları saklar ve seferi ilerletir. Konumlar kayıt zamanına (<c>RecordedAt</c>) göre değerlendirilir: seferin
/// en yeni konumundan eski (geç/sırasız gelen) konumlar yalnız rota için saklanır, durak varış/ayrılışını ve
/// seferin başlangıç/bitişini etkilemez. Saat sapması toleransı içindeki ileri tarihli konum "şimdi"ye kırpılır.
/// </summary>
internal sealed class IngestPingsHandler(
    TrackingDbContext db,
    ICurrentUser currentUser,
    IDriverTripFinder tripFinder,
    IStopProgressTracker stopTracker,
    ITripCloser tripCloser,
    ITripStatusPublisher statusPublisher,
    ILocationPingStore pingStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<TrackingOptions> options) : ICommandHandler<IngestPingsCommand, IngestPingsResult>
{
    public async Task<IngestPingsResult> Handle(IngestPingsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = timeProvider.GetUtcNow();
        PingBatchValidator.Validate(request.Pings, options.Value.MaxBatchSize, now);

        var driverUserId = currentUser.RequireUserId();

        // Aynı şoförün eş zamanlı paketleri sırayla işlenir: en yeni konum zamanı ve sefer durumu kilit altında okunur.
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        var lockKey = $"tracking-pings|{driverUserId:N}";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);

        var trip = await tripFinder.FindOpenTripAsync(driverUserId, cancellationToken);
        if (trip is null)
        {
            // KVKK: konum yalnızca açık bir sefer süresince tutulur.
            await unitOfWork.CommitAsync(cancellationToken);
            return new IngestPingsResult(0, null, null);
        }

        var samples = request.Pings
            .Select(p => new PingSample(p.Latitude, p.Longitude, p.AccuracyM, p.SpeedMps, p.Heading,
                p.RecordedAt > now ? now : p.RecordedAt))
            .OrderBy(p => p.RecordedAt)
            .ToList();

        // Seferin bilinen en yeni konumundan eski konumlar durak kararına katılmaz (son durumu geri almasın).
        var watermark = trip.StartedAt is { } startedAt
            ? await pingStore.GetLatestRecordedAtAsync(trip.Id, startedAt, cancellationToken)
            : null;
        var fresh = watermark is { } latest ? samples.Where(p => p.RecordedAt > latest).ToList() : samples;

        await pingStore.AddAsync(trip.Id, driverUserId, samples, cancellationToken);
        if (fresh.Count > 0)
        {
            if (trip.StartIfPlanned(fresh[0].RecordedAt))
            {
                await statusPublisher.PublishAsync(trip, fresh[0].RecordedAt, cancellationToken);
            }

            foreach (var sample in fresh)
            {
                stopTracker.Apply(trip, sample);
            }

            await tripCloser.CloseIfDoneAsync(trip, fresh[^1].RecordedAt, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return new IngestPingsResult(samples.Count, trip.Id, trip.Status);
    }
}
