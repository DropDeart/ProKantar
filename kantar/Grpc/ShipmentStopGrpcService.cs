using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.ShipmentStops;

namespace Kantar.Grpc;

/// <summary>
/// ShipmentStop entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class ShipmentStopGrpcService(ISender sender) : ShipmentStopService.ShipmentStopServiceBase
{
    public override async Task<ShipmentStopMessage> GetById(ShipmentStopByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetShipmentStopByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"ShipmentStop bulunamadı: {id}"));

        return new ShipmentStopMessage
        {
            Id = value.Id.ToString(),
            Sequence = value.Sequence,
            StopType = value.StopType.ToString(),
            Name = value.Name,
            Latitude = value.Latitude.ToString(CultureInfo.InvariantCulture),
            Longitude = value.Longitude.ToString(CultureInfo.InvariantCulture),
            RadiusM = value.RadiusM,
            PlannedAt = value.PlannedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
            Note = value.Note ?? string.Empty,
        };
    }
}
