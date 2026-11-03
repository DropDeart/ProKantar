using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.WeighingPhotos;

namespace Kantar.Grpc;

/// <summary>
/// WeighingPhoto entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class WeighingPhotoGrpcService(ISender sender) : WeighingPhotoService.WeighingPhotoServiceBase
{
    public override async Task<WeighingPhotoMessage> GetById(WeighingPhotoByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetWeighingPhotoByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"WeighingPhoto bulunamadı: {id}"));

        return new WeighingPhotoMessage
        {
            Id = value.Id.ToString(),
            Stage = value.Stage.ToString(),
            CameraName = value.CameraName ?? string.Empty,
            Url = value.Url,
            TakenAt = value.TakenAt.ToString("o", CultureInfo.InvariantCulture),
        };
    }
}
