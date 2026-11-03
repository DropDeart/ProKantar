using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.DeliveryLocations;

namespace Kantar.Grpc;

/// <summary>
/// DeliveryLocation entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class DeliveryLocationGrpcService(ISender sender) : DeliveryLocationService.DeliveryLocationServiceBase
{
    public override async Task<DeliveryLocationMessage> GetById(DeliveryLocationByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetDeliveryLocationByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"DeliveryLocation bulunamadı: {id}"));

        return new DeliveryLocationMessage
        {
            Id = value.Id.ToString(),
            Name = value.Name,
            Address = value.Address ?? string.Empty,
            Latitude = value.Latitude.ToString(CultureInfo.InvariantCulture),
            Longitude = value.Longitude.ToString(CultureInfo.InvariantCulture),
            RadiusM = value.RadiusM,
            ContactName = value.ContactName ?? string.Empty,
            ContactPhone = value.ContactPhone ?? string.Empty,
            IsActive = value.IsActive,
        };
    }
}
