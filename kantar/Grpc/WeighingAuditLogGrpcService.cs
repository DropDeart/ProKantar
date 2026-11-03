using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.WeighingAuditLogs;

namespace Kantar.Grpc;

/// <summary>
/// WeighingAuditLog entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class WeighingAuditLogGrpcService(ISender sender) : WeighingAuditLogService.WeighingAuditLogServiceBase
{
    public override async Task<WeighingAuditLogMessage> GetById(WeighingAuditLogByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetWeighingAuditLogByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"WeighingAuditLog bulunamadı: {id}"));

        return new WeighingAuditLogMessage
        {
            Id = value.Id.ToString(),
            Action = value.Action.ToString(),
            FieldName = value.FieldName ?? string.Empty,
            OldValue = value.OldValue ?? string.Empty,
            NewValue = value.NewValue ?? string.Empty,
            Reason = value.Reason ?? string.Empty,
        };
    }
}
