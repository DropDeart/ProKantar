using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Shipment.Status alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<ShipmentStatus>))]
public enum ShipmentStatus
{
    /// <summary>Draft.</summary>
    Draft,
    /// <summary>Dispatched.</summary>
    Dispatched,
    /// <summary>InProgress.</summary>
    InProgress,
    /// <summary>Completed.</summary>
    Completed,
    /// <summary>Cancelled.</summary>
    Cancelled,
}
