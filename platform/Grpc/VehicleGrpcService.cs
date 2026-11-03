using System.Globalization;
using Grpc.Core;
using MediatR;
using Platform.Features.Vehicles;

namespace Platform.Grpc;

/// <summary>
/// Vehicle entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class VehicleGrpcService(ISender sender) : VehicleService.VehicleServiceBase
{
    public override async Task<VehicleMessage> GetById(VehicleByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetVehicleByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Vehicle bulunamadı: {id}"));

        return new VehicleMessage
        {
            Id = value.Id.ToString(),
            OwnerUserId = value.OwnerUserId.ToString(),
            Plate = value.Plate,
            VehicleTypeCode = value.VehicleTypeCode ?? string.Empty,
            Brand = value.Brand ?? string.Empty,
            Model = value.Model ?? string.Empty,
            IsClaimed = value.IsClaimed,
        };
    }
}
