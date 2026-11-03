using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.TenantVehicles;

namespace Kantar.Grpc;

/// <summary>
/// TenantVehicle entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class TenantVehicleGrpcService(ISender sender) : TenantVehicleService.TenantVehicleServiceBase
{
    public override async Task<TenantVehicleMessage> GetById(TenantVehicleByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetTenantVehicleByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"TenantVehicle bulunamadı: {id}"));

        return new TenantVehicleMessage
        {
            Id = value.Id.ToString(),
            StoredTareKg = value.StoredTareKg?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            StoredTareAt = value.StoredTareAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
            IsBlacklisted = value.IsBlacklisted,
            BlacklistReason = value.BlacklistReason ?? string.Empty,
            Note = value.Note ?? string.Empty,
        };
    }
}
