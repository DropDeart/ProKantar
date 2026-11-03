using System.Globalization;
using Grpc.Core;
using MediatR;
using Platform.Features.Trailers;

namespace Platform.Grpc;

/// <summary>
/// Trailer entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class TrailerGrpcService(ISender sender) : TrailerService.TrailerServiceBase
{
    public override async Task<TrailerMessage> GetById(TrailerByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetTrailerByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Trailer bulunamadı: {id}"));

        return new TrailerMessage
        {
            Id = value.Id.ToString(),
            OwnerUserId = value.OwnerUserId.ToString(),
            Plate = value.Plate,
        };
    }
}
