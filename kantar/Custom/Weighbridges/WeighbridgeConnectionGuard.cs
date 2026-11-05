using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using BaseForge.Core.Exceptions;
using Kantar.Entities;
using Kantar.Features.Weighbridges;
using MediatR.Pipeline;

namespace Kantar.Custom.Weighbridges;

/// <summary>
/// Kantar bağlantı bilgisinin türüne uygunluğu: TCP/IP için IP adresi ve port zorunlu.
/// Bilgi <c>{"host":"192.168.1.20","port":4001}</c> biçiminde saklanır.
/// </summary>
internal static class WeighbridgeConnection
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>TCP için doğrular ve standart JSON'a çevirir; diğer türlerde bilgiyi olduğu gibi bırakır.</summary>
    public static string? Normalize(WeighbridgeConnectionType? type, string? info)
    {
        if (type != WeighbridgeConnectionType.Tcp)
        {
            return info;
        }

        TcpEndpoint? endpoint = null;
        try
        {
            endpoint = string.IsNullOrWhiteSpace(info) ? null : JsonSerializer.Deserialize<TcpEndpoint>(info, Json);
        }
        catch (JsonException)
        {
            // Aşağıda alan hatası olarak döner.
        }

        var host = endpoint?.Host?.Trim();
        if (string.IsNullOrEmpty(host) || !IPAddress.TryParse(host, out var ip) || ip.AddressFamily is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
        {
            throw new ValidationException("ConnectionInfo.Host", "TCP/IP bağlantısı için geçerli bir IP adresi girin.");
        }

        if (endpoint!.Port is not (>= 1 and <= 65535))
        {
            throw new ValidationException("ConnectionInfo.Port", "TCP/IP bağlantısı için 1–65535 arası bir port girin.");
        }

        return JsonSerializer.Serialize(new TcpEndpoint(ip.ToString(), endpoint.Port), Json);
    }

    private sealed record TcpEndpoint(string? Host, int? Port);
}

/// <summary>Kantar eklenirken bağlantı bilgisini doğrular.</summary>
internal sealed class CreateWeighbridgeConnectionGuard : IRequestPreProcessor<CreateWeighbridgeCommand>
{
    public Task Process(CreateWeighbridgeCommand request, CancellationToken cancellationToken)
    {
        request.ConnectionInfo = WeighbridgeConnection.Normalize(request.ConnectionType, request.ConnectionInfo);
        return Task.CompletedTask;
    }
}

/// <summary>Kantar güncellenirken bağlantı bilgisini doğrular.</summary>
internal sealed class UpdateWeighbridgeConnectionGuard : IRequestPreProcessor<UpdateWeighbridgeCommand>
{
    public Task Process(UpdateWeighbridgeCommand request, CancellationToken cancellationToken)
    {
        request.ConnectionInfo = WeighbridgeConnection.Normalize(request.ConnectionType, request.ConnectionInfo);
        return Task.CompletedTask;
    }
}
