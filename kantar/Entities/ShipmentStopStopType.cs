using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// ShipmentStop.StopType alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<ShipmentStopStopType>))]
public enum ShipmentStopStopType
{
    /// <summary>Pickup.</summary>
    Pickup,
    /// <summary>Weighbridge.</summary>
    Weighbridge,
    /// <summary>Delivery.</summary>
    Delivery,
}
