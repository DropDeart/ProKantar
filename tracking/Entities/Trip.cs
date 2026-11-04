using System.ComponentModel.DataAnnotations;
using BaseForge.Core.Entities;
using NetTopologySuite.Geometries;

namespace Tracking.Entities;

/// <summary>Bir sevkiyatın canlı takibi. Id = kantar Shipment Id.</summary>
public sealed class Trip : BaseEntity
{
    /// <summary>Sevkiyatın kiracısı.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Sevkiyat numarası.</summary>
    [MaxLength(30)]
    public string ShipmentNo { get; set; } = string.Empty;

    /// <summary>Gelen / giden.</summary>
    public TripDirection Direction { get; set; }

    /// <summary>Durum.</summary>
    public TripStatus Status { get; set; } = TripStatus.Planned;

    /// <summary>kantar/Station.</summary>
    public Guid StationId { get; set; }

    /// <summary>platform/Vehicle.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Plaka.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;

    /// <summary>platform/Driver.</summary>
    public Guid DriverId { get; set; }

    /// <summary>Şoförün kullanıcı kimliği (token sub); konumu bu eşler.</summary>
    public Guid DriverUserId { get; set; }

    /// <summary>Şoför adı.</summary>
    [MaxLength(150)]
    public string? DriverName { get; set; }

    /// <summary>Planlanan zaman.</summary>
    public DateTimeOffset PlannedAt { get; set; }

    /// <summary>İlk konumun zamanı.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>Bitiş zamanı.</summary>
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>Tamamlanınca konumlardan çizilen rota (ham konumlar silinse de kalır).</summary>
    public LineString? Route { get; set; }

    /// <summary>Rota uzunluğu (m).</summary>
    public double? DistanceM { get; set; }

    /// <summary>Duraklar.</summary>
    public List<TripStop> Stops { get; set; } = [];

    /// <summary>Konum kabul ediyor mu?</summary>
    public bool IsOpen => Status is TripStatus.Planned or TripStatus.Active;

    /// <summary>Planlanmış seferi ilk konumla başlatır.</summary>
    public void StartIfPlanned(DateTimeOffset at)
    {
        if (Status != TripStatus.Planned)
        {
            return;
        }

        Status = TripStatus.Active;
        StartedAt = at;
    }

    /// <summary>Tüm duraklar bittiyse seferi kapatır; kapattıysa true.</summary>
    public bool CompleteIfAllStopsDone(DateTimeOffset at)
    {
        if (!IsOpen || Stops.Count == 0 || !Stops.TrueForAll(s => s.IsDone))
        {
            return false;
        }

        Status = TripStatus.Completed;
        EndedAt = at;
        return true;
    }
}
