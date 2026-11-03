using System.Globalization;
using Grpc.Core;
using MediatR;
using Platform.Features.Devices;

namespace Platform.Grpc;

/// <summary>
/// Device entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class DeviceGrpcService(ISender sender) : DeviceService.DeviceServiceBase
{
    public override async Task<DeviceMessage> GetById(DeviceByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetDeviceByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Device bulunamadı: {id}"));

        return new DeviceMessage
        {
            Id = value.Id.ToString(),
            UserId = value.UserId.ToString(),
            DeviceIdentifier = value.DeviceIdentifier,
            Platform = value.Platform.ToString(),
            PushToken = value.PushToken ?? string.Empty,
            AppVersion = value.AppVersion ?? string.Empty,
            LastSeenAt = value.LastSeenAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }
}
