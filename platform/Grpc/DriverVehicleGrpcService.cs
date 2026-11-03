using System.Globalization;
using Grpc.Core;
using MediatR;
using Platform.Features.DriverVehicles;

namespace Platform.Grpc;

/// <summary>
/// DriverVehicle entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class DriverVehicleGrpcService(ISender sender) : DriverVehicleService.DriverVehicleServiceBase
{
    public override async Task<DriverVehicleMessage> GetById(DriverVehicleByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetDriverVehicleByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"DriverVehicle bulunamadı: {id}"));

        return new DriverVehicleMessage
        {
            Id = value.Id.ToString(),
            UserId = value.UserId.ToString(),
            StartedAt = value.StartedAt.ToString("o", CultureInfo.InvariantCulture),
            EndedAt = value.EndedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }
}
