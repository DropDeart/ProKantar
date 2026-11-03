using System.Globalization;
using Grpc.Core;
using MediatR;
using Platform.Features.Drivers;

namespace Platform.Grpc;

/// <summary>
/// Driver entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class DriverGrpcService(ISender sender) : DriverService.DriverServiceBase
{
    public override async Task<DriverMessage> GetById(DriverByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetDriverByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Driver bulunamadı: {id}"));

        return new DriverMessage
        {
            Id = value.Id.ToString(),
            UserId = value.UserId.ToString(),
            FullName = value.FullName,
            Phone = value.Phone,
            NationalId = value.NationalId ?? string.Empty,
            LicenseNo = value.LicenseNo ?? string.Empty,
            KvkkConsentAt = value.KvkkConsentAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
            LocationConsent = value.LocationConsent,
            IsActive = value.IsActive,
        };
    }
}
