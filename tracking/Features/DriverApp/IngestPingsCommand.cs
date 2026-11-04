using BaseForge.Core.CQRS;
using BaseForge.Core.Interfaces;
using Microsoft.Extensions.Options;
using Tracking.Common;
using Tracking.Configuration;
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

internal sealed class IngestPingsHandler(
    ICurrentUser currentUser,
    IDriverTripFinder tripFinder,
    IStopProgressTracker stopTracker,
    ITripCloser tripCloser,
    ILocationPingStore pingStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<TrackingOptions> options) : ICommandHandler<IngestPingsCommand, IngestPingsResult>
{
    public async Task<IngestPingsResult> Handle(IngestPingsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        PingBatchValidator.Validate(request.Pings, options.Value.MaxBatchSize, timeProvider.GetUtcNow());

        var driverUserId = currentUser.RequireUserId();
        var trip = await tripFinder.FindOpenTripAsync(driverUserId, cancellationToken);
        if (trip is null)
        {
            // KVKK: konum yalnızca açık bir sefer süresince tutulur.
            return new IngestPingsResult(0, null, null);
        }

        var samples = request.Pings
            .OrderBy(p => p.RecordedAt)
            .Select(p => new PingSample(p.Latitude, p.Longitude, p.AccuracyM, p.SpeedMps, p.Heading, p.RecordedAt))
            .ToList();

        trip.StartIfPlanned(samples[0].RecordedAt);
        foreach (var sample in samples)
        {
            stopTracker.Apply(trip, sample);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await pingStore.AddAsync(trip.Id, driverUserId, samples, cancellationToken);
        await tripCloser.CloseIfDoneAsync(trip, samples[^1].RecordedAt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return new IngestPingsResult(samples.Count, trip.Id, trip.Status);
    }
}
