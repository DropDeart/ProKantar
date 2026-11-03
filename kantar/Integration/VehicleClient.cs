using System.Globalization;
using Grpc.Core;
using Platform.Grpc;

namespace Kantar.Integration;

/// <summary>platform/Vehicle servisine senkron (gRPC) erişim sözleşmesi. BaseForge.CodeGen tarafından üretildi.</summary>
public interface IVehicleClient
{
    /// <summary>Uzak servisten Vehicle kaydını getirir; bulunamazsa <see langword="null"/>.</summary>
    Task<VehicleReference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Uzak Vehicle kaydının yerel (zengin) görünümü.</summary>
public sealed class VehicleReference
{
    /// <summary>Uzak kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Plate.</summary>
    public string Plate { get; set; } = string.Empty;
    /// <summary>VehicleTypeCode.</summary>
    public string? VehicleTypeCode { get; set; }
    /// <summary>Brand.</summary>
    public string? Brand { get; set; }
    /// <summary>Model.</summary>
    public string? Model { get; set; }
    /// <summary>IsClaimed.</summary>
    public bool IsClaimed { get; set; }
}

/// <summary><see cref="IVehicleClient"/>'in Platform servisine gRPC ile bağlanan gerçek implementasyonu.</summary>
public sealed class VehicleClient(VehicleService.VehicleServiceClient client) : IVehicleClient
{
    public async Task<VehicleReference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.GetByIdAsync(
                new VehicleByIdRequest { Id = id.ToString() },
                cancellationToken: cancellationToken);

            return new VehicleReference
            {
                Id = Guid.Parse(response.Id),
                OwnerUserId = Guid.Parse(response.OwnerUserId),
                Plate = response.Plate,
                VehicleTypeCode = string.IsNullOrEmpty(response.VehicleTypeCode) ? null : response.VehicleTypeCode,
                Brand = string.IsNullOrEmpty(response.Brand) ? null : response.Brand,
                Model = string.IsNullOrEmpty(response.Model) ? null : response.Model,
                IsClaimed = response.IsClaimed,
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
