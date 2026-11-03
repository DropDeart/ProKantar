using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.Stations;

namespace Kantar.Grpc;

/// <summary>
/// Station entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class StationGrpcService(ISender sender) : StationService.StationServiceBase
{
    public override async Task<StationMessage> GetById(StationByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetStationByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Station bulunamadı: {id}"));

        return new StationMessage
        {
            Id = value.Id.ToString(),
            Name = value.Name,
            Code = value.Code,
            Address = value.Address ?? string.Empty,
            Phone = value.Phone ?? string.Empty,
            Latitude = value.Latitude?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Longitude = value.Longitude?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            RadiusM = value.RadiusM,
            IsActive = value.IsActive,
        };
    }
}
