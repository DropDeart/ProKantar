using Grpc.Core;
using Platform.Grpc;

namespace Tracking.Integration;

/// <summary>platform/Driver'a senkron (gRPC) erişim.</summary>
public interface IDriverClient
{
    /// <summary>Şoförün kullanıcı kimliği (identity sub); şoför yoksa null.</summary>
    Task<Guid?> GetUserIdAsync(Guid driverId, CancellationToken cancellationToken = default);
}

/// <summary>Platform gRPC ile <see cref="IDriverClient"/>.</summary>
internal sealed class DriverClient(DriverService.DriverServiceClient client) : IDriverClient
{
    public async Task<Guid?> GetUserIdAsync(Guid driverId, CancellationToken cancellationToken = default)
    {
        try
        {
            var driver = await client.GetByIdAsync(
                new DriverByIdRequest { Id = driverId.ToString() },
                cancellationToken: cancellationToken);
            return Guid.Parse(driver.UserId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
