using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Weighing.Status alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<WeighingStatus>))]
public enum WeighingStatus
{
    /// <summary>Open.</summary>
    Open,
    /// <summary>Completed.</summary>
    Completed,
    /// <summary>Cancelled.</summary>
    Cancelled,
}
