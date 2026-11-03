using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.VehicleTypes;

namespace Kantar.Grpc;

/// <summary>
/// VehicleType entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class VehicleTypeGrpcService(ISender sender) : VehicleTypeService.VehicleTypeServiceBase
{
    public override async Task<VehicleTypeMessage> GetById(VehicleTypeByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetVehicleTypeByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"VehicleType bulunamadı: {id}"));

        return new VehicleTypeMessage
        {
            Id = value.Id.ToString(),
            Code = value.Code,
            Name = value.Name,
            MaxGrossKg = value.MaxGrossKg?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }
}
