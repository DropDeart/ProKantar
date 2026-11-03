using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Platform.Entities;

/// <summary>
/// Device.Platform alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<DevicePlatform>))]
public enum DevicePlatform
{
    /// <summary>Android.</summary>
    Android,
    /// <summary>Ios.</summary>
    Ios,
}
