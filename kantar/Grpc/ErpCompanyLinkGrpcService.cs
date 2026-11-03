using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.ErpCompanyLinks;

namespace Kantar.Grpc;

/// <summary>
/// ErpCompanyLink entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class ErpCompanyLinkGrpcService(ISender sender) : ErpCompanyLinkService.ErpCompanyLinkServiceBase
{
    public override async Task<ErpCompanyLinkMessage> GetById(ErpCompanyLinkByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetErpCompanyLinkByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"ErpCompanyLink bulunamadı: {id}"));

        return new ErpCompanyLinkMessage
        {
            Id = value.Id.ToString(),
            ErpSystem = value.ErpSystem,
            ErpCode = value.ErpCode,
            IsDefault = value.IsDefault,
        };
    }
}
