using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// WeighingAuditLog.Action alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<WeighingAuditLogAction>))]
public enum WeighingAuditLogAction
{
    /// <summary>ManualWeight.</summary>
    ManualWeight,
    /// <summary>TareChange.</summary>
    TareChange,
    /// <summary>Cancel.</summary>
    Cancel,
    /// <summary>Edit.</summary>
    Edit,
}
