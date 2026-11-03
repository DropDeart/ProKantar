using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Waybill.WaybillType alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<WaybillWaybillType>))]
public enum WaybillWaybillType
{
    /// <summary>Paper.</summary>
    Paper,
    /// <summary>EDespatch.</summary>
    EDespatch,
}
