using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Kantar.Entities;

/// <summary>
/// Waybill.EDespatchStatus alanının izinli değerleri (BaseForge.CodeGen tarafından üretildi). JSON'da (API, olaylar) ve
/// veritabanında sayı değil, değer adıyla taşınır; sayısal değerler reddedilir.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<WaybillEDespatchStatus>))]
public enum WaybillEDespatchStatus
{
    /// <summary>Draft.</summary>
    Draft,
    /// <summary>Sent.</summary>
    Sent,
    /// <summary>Accepted.</summary>
    Accepted,
    /// <summary>Rejected.</summary>
    Rejected,
    /// <summary>AwaitingManualUpload.</summary>
    AwaitingManualUpload,
    /// <summary>Delivered.</summary>
    Delivered,
    /// <summary>Failed.</summary>
    Failed,
}
