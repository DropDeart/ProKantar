using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Tracking.Common;
using Tracking.Data;
using Tracking.Domain;
using Tracking.Entities;

namespace Tracking.Features.DriverApp;

/// <summary>Şoför durağı onaylar ("Teslim ettim").</summary>
public sealed class ConfirmStopCommand : ICommand
{
    /// <summary>Sefer.</summary>
    public Guid TripId { get; set; }

    /// <summary>Durak.</summary>
    public Guid StopId { get; set; }
}

internal sealed class ConfirmStopHandler(
    TrackingDbContext db,
    ICurrentUser currentUser,
    ITripCloser tripCloser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<ConfirmStopCommand>
{
    public async Task Handle(ConfirmStopCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var driverUserId = currentUser.RequireUserId();

        // Başka şoförün seferi 404 döner (varlığı sızdırılmaz).
        var trip = await db.Trips
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DriverUserId == driverUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        if (!trip.IsOpen)
        {
            throw new ValidationException("tripId", "Sefer kapalı; durak onaylanamaz.");
        }

        var stop = trip.Stops.Find(s => s.Id == request.StopId)
            ?? throw new NotFoundException(nameof(TripStop), request.StopId);
        if (stop.Status == StopStatus.Skipped)
        {
            throw new ValidationException("stopId", "Plandan çıkarılmış durak onaylanamaz.");
        }

        var now = timeProvider.GetUtcNow();
        stop.Confirm(now);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await tripCloser.CloseIfDoneAsync(trip, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
