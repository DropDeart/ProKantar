using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.Waybills;

namespace Kantar.Grpc;

/// <summary>
/// Waybill entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class WaybillGrpcService(ISender sender) : WaybillService.WaybillServiceBase
{
    public override async Task<WaybillMessage> GetById(WaybillByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetWaybillByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Waybill bulunamadı: {id}"));

        return new WaybillMessage
        {
            Id = value.Id.ToString(),
            WaybillNo = value.WaybillNo,
            WaybillType = value.WaybillType.ToString(),
            IssuedAt = value.IssuedAt.ToString("o", CultureInfo.InvariantCulture),
            DeliveryAddress = value.DeliveryAddress ?? string.Empty,
            EDespatchUuid = value.EDespatchUuid?.ToString() ?? string.Empty,
            EDespatchStatus = value.EDespatchStatus?.ToString() ?? string.Empty,
            ErpSyncStatus = value.ErpSyncStatus.ToString(),
            ErpDocNo = value.ErpDocNo ?? string.Empty,
            ErpSyncedAt = value.ErpSyncedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
            ErpError = value.ErpError ?? string.Empty,
        };
    }
}
