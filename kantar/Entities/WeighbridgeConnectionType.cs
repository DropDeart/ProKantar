using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Weighbridge.ConnectionType alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<WeighbridgeConnectionType>))]
public enum WeighbridgeConnectionType
{
    /// <summary>Serial.</summary>
    Serial,
    /// <summary>Tcp.</summary>
    Tcp,
}
