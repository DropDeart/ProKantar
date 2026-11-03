using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Shipment.Direction alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<ShipmentDirection>))]
public enum ShipmentDirection
{
    /// <summary>Inbound.</summary>
    Inbound,
    /// <summary>Outbound.</summary>
    Outbound,
}
