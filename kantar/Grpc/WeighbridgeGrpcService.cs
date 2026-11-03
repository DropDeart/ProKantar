using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.Weighbridges;

namespace Kantar.Grpc;

/// <summary>
/// Weighbridge entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class WeighbridgeGrpcService(ISender sender) : WeighbridgeService.WeighbridgeServiceBase
{
    public override async Task<WeighbridgeMessage> GetById(WeighbridgeByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetWeighbridgeByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Weighbridge bulunamadı: {id}"));

        return new WeighbridgeMessage
        {
            Id = value.Id.ToString(),
            Name = value.Name,
            SerialNo = value.SerialNo ?? string.Empty,
            CapacityKg = value.CapacityKg.ToString(CultureInfo.InvariantCulture),
            ConnectionType = value.ConnectionType?.ToString() ?? string.Empty,
            ConnectionInfo = value.ConnectionInfo ?? string.Empty,
            CalibrationValidUntil = value.CalibrationValidUntil?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            IsActive = value.IsActive,
        };
    }
}
