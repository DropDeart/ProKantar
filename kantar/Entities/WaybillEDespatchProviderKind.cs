using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Waybill.EDespatchProviderKind alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<WaybillEDespatchProviderKind>))]
public enum WaybillEDespatchProviderKind
{
    /// <summary>PaperOnly.</summary>
    PaperOnly,
    /// <summary>ManualXml.</summary>
    ManualXml,
    /// <summary>ELogo.</summary>
    ELogo,
}
