using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Weighing.TareSource alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<WeighingTareSource>))]
public enum WeighingTareSource
{
    /// <summary>Measured.</summary>
    Measured,
    /// <summary>Stored.</summary>
    Stored,
}
