using System.ComponentModel.DataAnnotations;
using BaseForge.Core.Entities;
using NetTopologySuite.Geometries;

namespace Tracking.Entities;

/// <summary>Seferin bir durağı. Id = kantar ShipmentStop Id.</summary>
public sealed class TripStop : BaseEntity
{
    /// <summary>Bağlı sefer.</summary>
    public Guid TripId { get; set; }

    /// <summary>1'den başlayan sıra.</summary>
    public int Sequence { get; set; }

    /// <summary>Durak tipi.</summary>
    public StopType StopType { get; set; }

    /// <summary>Lokasyon / istasyon adı.</summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Konum (WGS84, SRID 4326).</summary>
    public Point Location { get; set; } = null!;

    /// <summary>Varış yarıçapı (m).</summary>
    public int RadiusM { get; set; }

    /// <summary>Planlanan varış.</summary>
    public DateTimeOffset? PlannedAt { get; set; }

    /// <summary>kantar/Waybill (teslimat durağında).</summary>
    public Guid? WaybillId { get; set; }

    /// <summary>Konuma göre durum.</summary>
    public StopStatus Status { get; set; } = StopStatus.Pending;

    /// <summary>Varış zamanı.</summary>
    public DateTimeOffset? ArrivedAt { get; set; }

    /// <summary>Ayrılış zamanı.</summary>
    public DateTimeOffset? DepartedAt { get; set; }

    /// <summary>Onay zamanı: teslimatta şoför, kantarda tartım tamamlanınca.</summary>
    public DateTimeOffset? ConfirmedAt { get; set; }

    /// <summary>Durak bitti mi?</summary>
    public bool IsDone => Status is StopStatus.Departed or StopStatus.Skipped || ConfirmedAt is not null;

    /// <summary>Durağa varıldı.</summary>
    public void MarkArrived(DateTimeOffset at)
    {
        Status = StopStatus.Arrived;
        ArrivedAt = at;
    }

    /// <summary>Duraktan ayrıldı.</summary>
    public void MarkDeparted(DateTimeOffset at)
    {
        Status = StopStatus.Departed;
        DepartedAt = at;
    }

    /// <summary>Durağı onaylar (ilk onay geçerlidir).</summary>
    public void Confirm(DateTimeOffset at) => ConfirmedAt ??= at;
}
