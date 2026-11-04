using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;

namespace Tracking.Entities;

/// <summary>Seferin yönü. Değer adları kantar'daki ShipmentDirection ile aynıdır.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<TripDirection>))]
public enum TripDirection
{
    /// <summary>Kantara gelen araç.</summary>
    Inbound,

    /// <summary>Kantardan çıkan araç.</summary>
    Outbound,
}

/// <summary>Sefer durumu.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<TripStatus>))]
public enum TripStatus
{
    /// <summary>Planlandı, henüz konum gelmedi.</summary>
    Planned,

    /// <summary>Yolda.</summary>
    Active,

    /// <summary>Tüm duraklar bitti.</summary>
    Completed,

    /// <summary>İptal edildi.</summary>
    Cancelled,
}

/// <summary>Durak tipi. Değer adları kantar'daki ShipmentStopStopType ile aynıdır.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<StopType>))]
public enum StopType
{
    /// <summary>Yükleme noktası.</summary>
    Pickup,

    /// <summary>Kantar.</summary>
    Weighbridge,

    /// <summary>Teslimat noktası.</summary>
    Delivery,
}

/// <summary>Durağın konuma göre durumu.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<StopStatus>))]
public enum StopStatus
{
    /// <summary>Henüz varılmadı.</summary>
    Pending,

    /// <summary>Durak yarıçapı içinde.</summary>
    Arrived,

    /// <summary>Duraktan ayrıldı.</summary>
    Departed,

    /// <summary>Atlandı (plan değişti).</summary>
    Skipped,
}
