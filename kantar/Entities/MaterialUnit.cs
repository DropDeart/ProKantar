using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Material.Unit alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<MaterialUnit>))]
public enum MaterialUnit
{
    /// <summary>Kg.</summary>
    Kg,
    /// <summary>Ton.</summary>
    Ton,
    /// <summary>M3.</summary>
    M3,
    /// <summary>Piece.</summary>
    Piece,
}
