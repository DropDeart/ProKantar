using System.Globalization;
using Grpc.Core;
using MediatR;
using Platform.Features.Carriers;

namespace Platform.Grpc;

/// <summary>
/// Carrier entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class CarrierGrpcService(ISender sender) : CarrierService.CarrierServiceBase
{
    public override async Task<CarrierMessage> GetById(CarrierByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetCarrierByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Carrier bulunamadı: {id}"));

        return new CarrierMessage
        {
            Id = value.Id.ToString(),
            OwnerUserId = value.OwnerUserId.ToString(),
            Name = value.Name,
            TaxNo = value.TaxNo ?? string.Empty,
            TaxOffice = value.TaxOffice ?? string.Empty,
            Phone = value.Phone ?? string.Empty,
            Address = value.Address ?? string.Empty,
        };
    }
}
