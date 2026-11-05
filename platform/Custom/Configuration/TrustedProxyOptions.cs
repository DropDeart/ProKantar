using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Platform.Custom.Configuration;

/// <summary>X-Forwarded-* başlıklarına güvenilecek reverse proxy'ler (<c>ForwardedHeaders</c> bölümü).</summary>
public sealed class TrustedProxyOptions
{
    /// <summary>Yapılandırma bölümü.</summary>
    public const string SectionName = "ForwardedHeaders";

    /// <summary>Tek tek proxy IP'leri (örn. <c>172.17.0.1</c>).</summary>
    public List<string> KnownProxies { get; set; } = [];

    /// <summary>Proxy ağları, CIDR (örn. <c>172.18.0.0/16</c>).</summary>
    public List<string> KnownNetworks { get; set; } = [];
}

/// <summary>Forwarded headers middleware'ini yalnız güvenilir proxy'lere açar.</summary>
public static class TrustedProxyExtensions
{
    /// <summary>
    /// X-Forwarded-For/Proto yalnız loopback ve yapılandırılmış proxy/ağlardan gelirse uygulanır; diğer
    /// kaynaklarda başlık yok sayılır ve istemci IP'si <c>Connection.RemoteIpAddress</c> olarak kalır.
    /// </summary>
    public static IApplicationBuilder UseTrustedForwardedHeaders(this IApplicationBuilder app, IConfiguration configuration)
    {
        var trusted = configuration.GetSection(TrustedProxyOptions.SectionName).Get<TrustedProxyOptions>() ?? new TrustedProxyOptions();
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,   // yalnız son (güvenilir) hop'un eklediği değer
        };

        foreach (var proxy in trusted.KnownProxies.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            options.KnownProxies.Add(IPAddress.TryParse(proxy.Trim(), out var address) ? address : throw Invalid(proxy));
        }

        foreach (var network in trusted.KnownNetworks.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.TryParse(network.Trim(), out var parsed) ? parsed : throw Invalid(network));
        }

        return app.UseForwardedHeaders(options);
    }

    // Hatalı girdi sessizce yok sayılmaz: güvenlik ayarı başlangıçta patlar.
    private static InvalidOperationException Invalid(string value)
        => new($"{TrustedProxyOptions.SectionName}: geçersiz adres '{value}'.");
}
