using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.Companys;

namespace Kantar.Grpc;

/// <summary>
/// Company entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class CompanyGrpcService(ISender sender) : CompanyService.CompanyServiceBase
{
    public override async Task<CompanyMessage> GetById(CompanyByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetCompanyByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Company bulunamadı: {id}"));

        return new CompanyMessage
        {
            Id = value.Id.ToString(),
            Name = value.Name,
            ShortName = value.ShortName ?? string.Empty,
            TaxNo = value.TaxNo ?? string.Empty,
            TaxOffice = value.TaxOffice ?? string.Empty,
            IsCustomer = value.IsCustomer,
            IsSupplier = value.IsSupplier,
            IsCarrier = value.IsCarrier,
            Phone = value.Phone ?? string.Empty,
            Email = value.Email ?? string.Empty,
            Address = value.Address ?? string.Empty,
            IsActive = value.IsActive,
        };
    }
}
