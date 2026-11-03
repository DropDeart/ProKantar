using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// WeighingPhoto.Stage alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<WeighingPhotoStage>))]
public enum WeighingPhotoStage
{
    /// <summary>First.</summary>
    First,
    /// <summary>Second.</summary>
    Second,
}
