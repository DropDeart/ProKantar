using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.Shipments;

namespace Kantar.Grpc;

/// <summary>
/// Shipment entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class ShipmentGrpcService(ISender sender) : ShipmentService.ShipmentServiceBase
{
    public override async Task<ShipmentMessage> GetById(ShipmentByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetShipmentByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Shipment bulunamadı: {id}"));

        return new ShipmentMessage
        {
            Id = value.Id.ToString(),
            ShipmentNo = value.ShipmentNo,
            Direction = value.Direction.ToString(),
            Status = value.Status.ToString(),
            PlannedAt = value.PlannedAt.ToString("o", CultureInfo.InvariantCulture),
            Plate = value.Plate,
            DriverId = value.DriverId?.ToString() ?? string.Empty,
            DriverName = value.DriverName ?? string.Empty,
            DispatchedAt = value.DispatchedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
            Note = value.Note ?? string.Empty,
        };
    }
}
