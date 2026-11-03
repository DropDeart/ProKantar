using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Waybill.ErpSyncStatus alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<WaybillErpSyncStatus>))]
public enum WaybillErpSyncStatus
{
    /// <summary>Pending.</summary>
    Pending,
    /// <summary>Sent.</summary>
    Sent,
    /// <summary>Failed.</summary>
    Failed,
}
