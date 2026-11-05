using BaseForge.API.Authentication;
using BaseForge.Core.Exceptions;
using Grpc.Core;
using Identity.Grpc;
using Platform.Grpc;

namespace Kantar.Custom.EDespatch.Parties;

/// <summary>Gönderen (kiracı) künyesi kaynağı.</summary>
public interface ISupplierDirectory
{
    /// <summary>Kiracının e-İrsaliye gönderen bilgisi; şirket yoksa null.</summary>
    Task<DespatchParty?> GetAsync(Guid tenantId, CancellationToken cancellationToken);
}

/// <summary>Platform şoför kaydının irsaliyeye giren kısmı.</summary>
/// <param name="FullName">Ad soyad.</param>
/// <param name="NationalId">TCKN (platformda yoksa null).</param>
public sealed record DriverRecord(string FullName, string? NationalId);

/// <summary>Şoför kaydı kaynağı.</summary>
public interface IDriverDirectory
{
    /// <summary>Şoför; platformda yoksa null.</summary>
    Task<DriverRecord?> GetAsync(Guid driverId, CancellationToken cancellationToken);
}

/// <summary>
/// Servis jetonlu (bf_svc) çağrı başlığı. Kullanıcı jetonu bilinçli olarak iletilmez: identity şirket künyesi ve
/// platform TCKN uçları yalnız servis jetonunu kabul eder.
/// </summary>
internal static class ServiceCall
{
    /// <summary>Authorization başlığı; servis istemcisi yapılandırılmamışsa hata.</summary>
    public static async Task<Metadata> HeadersAsync(IServiceTokenProvider tokens, CancellationToken ct)
    {
        var token = await tokens.GetTokenAsync(ct)
            ?? throw new InvalidOperationException("Servis jetonu alınamadı: Auth:ServiceClient:ClientSecret tanımlı değil.");
        return new Metadata { { "authorization", "Bearer " + token } };
    }
}

/// <summary>Identity <c>TenantService</c> üzerinden gönderen künyesi.</summary>
internal sealed class SupplierDirectory(TenantService.TenantServiceClient client, IServiceTokenProvider tokens, ILogger<SupplierDirectory> logger)
    : ISupplierDirectory
{
    /// <inheritdoc />
    public async Task<DespatchParty?> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        TenantMessage tenant;
        try
        {
            tenant = await client.GetByIdAsync(new TenantByIdRequest { Id = tenantId.ToString() },
                await ServiceCall.HeadersAsync(tokens, cancellationToken), cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "Şirket künyesi identity'den alınamadı ({TenantId}).", tenantId);
            throw new ValidationException("Supplier", "Şirket bilgisi alınamadı; biraz sonra tekrar deneyin.");
        }

        return new DespatchParty
        {
            TaxId = tenant.TaxNo,
            Name = tenant.Name,
            TaxOffice = Blank(tenant.TaxOffice),
            Phone = Blank(tenant.Phone),
            Email = Blank(tenant.Email),
            Address = new DespatchAddress
            {
                Street = Blank(tenant.Address),
                District = Blank(tenant.District),
                City = Blank(tenant.City),
                PostalCode = Blank(tenant.PostalCode),
            },
        };
    }

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Platform <c>DriverService</c> üzerinden şoför adı ve TCKN.</summary>
internal sealed class DriverDirectory(DriverService.DriverServiceClient client, IServiceTokenProvider tokens, ILogger<DriverDirectory> logger)
    : IDriverDirectory
{
    /// <inheritdoc />
    public async Task<DriverRecord?> GetAsync(Guid driverId, CancellationToken cancellationToken)
    {
        try
        {
            var driver = await client.GetByIdAsync(new DriverByIdRequest { Id = driverId.ToString() },
                await ServiceCall.HeadersAsync(tokens, cancellationToken), cancellationToken: cancellationToken);
            return new DriverRecord(driver.FullName, string.IsNullOrWhiteSpace(driver.NationalId) ? null : driver.NationalId.Trim());
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "Şoför kaydı platformdan alınamadı ({DriverId}).", driverId);
            throw new ValidationException("Driver", "Şoför bilgisi alınamadı; biraz sonra tekrar deneyin.");
        }
    }
}
