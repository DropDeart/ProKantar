using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.Materials;

namespace Kantar.Grpc;

/// <summary>
/// Material entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class MaterialGrpcService(ISender sender) : MaterialService.MaterialServiceBase
{
    public override async Task<MaterialMessage> GetById(MaterialByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetMaterialByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Material bulunamadı: {id}"));

        return new MaterialMessage
        {
            Id = value.Id.ToString(),
            Code = value.Code,
            Name = value.Name,
            Unit = value.Unit.ToString(),
            KgPerUnit = value.KgPerUnit?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ErpStockCode = value.ErpStockCode ?? string.Empty,
            IsActive = value.IsActive,
        };
    }
}
