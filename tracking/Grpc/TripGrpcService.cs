using System.Globalization;
using Grpc.Core;
using MediatR;
using Tracking.Features.Trips;

namespace Tracking.Grpc;

/// <summary>Seferin diğer servislere salt-okunur gRPC erişimi (CQRS sorgusu üzerinden).</summary>
public sealed class TripGrpcService(ISender sender) : TripService.TripServiceBase
{
    /// <inheritdoc />
    public override async Task<TripMessage> GetById(TripByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var trip = await sender.Send(new GetTripByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Sefer bulunamadı: {id}"));

        var message = new TripMessage
        {
            Id = trip.Id.ToString(),
            ShipmentNo = trip.ShipmentNo,
            Direction = trip.Direction.ToString(),
            Status = trip.Status.ToString(),
            Plate = trip.Plate,
            DriverId = trip.DriverId.ToString(),
            PlannedAt = Format(trip.PlannedAt),
            StartedAt = Format(trip.StartedAt),
            EndedAt = Format(trip.EndedAt),
            DistanceM = trip.DistanceM ?? 0,
        };
        message.Stops.AddRange(trip.Stops.Select(s => new TripStopMessage
        {
            Id = s.Id.ToString(),
            Sequence = s.Sequence,
            StopType = s.StopType.ToString(),
            Name = s.Name,
            Status = s.Status.ToString(),
            ArrivedAt = Format(s.ArrivedAt),
            DepartedAt = Format(s.DepartedAt),
            ConfirmedAt = Format(s.ConfirmedAt),
        }));
        return message;
    }

    private static string Format(DateTimeOffset? value)
        => value?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty;
}
