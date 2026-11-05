using Grpc.Core;
using Identity.Data;
using Identity.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Identity.Grpc;

/// <summary>
/// Şirket künyesine salt-okunur gRPC erişimi (e-İrsaliye gönderen bilgisi). Yalnız servis jetonuyla eşlenir;
/// çağıran servis kiracı kimliğini kendi doğruladığı kullanıcı jetonundan verir.
/// </summary>
public sealed class TenantGrpcService(IdentityServiceDbContext db) : TenantService.TenantServiceBase
{
    /// <summary>Şirketi kimliğiyle getirir; yoksa NotFound.</summary>
    public override async Task<TenantMessage> GetById(TenantByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var tenant = await db.Set<Tenant>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Şirket bulunamadı: {id}"));

        return new TenantMessage
        {
            Id = tenant.Id.ToString(),
            Name = tenant.Name,
            TaxNo = tenant.TaxNo ?? string.Empty,
            TaxOffice = tenant.TaxOffice ?? string.Empty,
            Phone = tenant.Phone ?? string.Empty,
            Email = tenant.Email ?? string.Empty,
            City = tenant.City ?? string.Empty,
            District = tenant.District ?? string.Empty,
            PostalCode = tenant.PostalCode ?? string.Empty,
            Address = tenant.Address ?? string.Empty,
            Status = tenant.Status.ToString(),
        };
    }
}
