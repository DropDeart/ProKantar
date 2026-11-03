using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Weighing.Direction alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<WeighingDirection>))]
public enum WeighingDirection
{
    /// <summary>Inbound.</summary>
    Inbound,
    /// <summary>Outbound.</summary>
    Outbound,
}
