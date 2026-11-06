using System.ComponentModel.DataAnnotations;
using BaseForge.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Entities;

/// <summary>
/// Teslimat/yükleme durağının yük satırı (elle yazıldı; generate edilmez). Yalnız sevkiyat planıyla
/// (<c>SaveShipmentPlanCommand</c>) yazılır; malzeme adı ve kodu plan anındaki haliyle saklanır (snapshot).
/// </summary>
public sealed class ShipmentStopLine : BaseEntity, ITenantEntity
{
    /// <summary>Durak içindeki sıra (1'den başlar).</summary>
    public int Sequence { get; set; }

    /// <summary>Bağlı durak.</summary>
    public Guid ShipmentStopId { get; set; }

    /// <summary>Malzeme.</summary>
    public Guid MaterialId { get; set; }

    /// <summary>Malzeme kodu (snapshot).</summary>
    [MaxLength(50)]
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>Malzeme adı (snapshot).</summary>
    [MaxLength(200)]
    public string MaterialName { get; set; } = string.Empty;

    /// <summary>Planlanan miktar (<see cref="Unit"/> cinsinden).</summary>
    public decimal PlannedQuantity { get; set; }

    /// <summary>Miktar birimi.</summary>
    public MaterialUnit Unit { get; set; }

    /// <summary>Kiracı.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Durak (servis içi ilişki).</summary>
    public ShipmentStop? ShipmentStop { get; set; }

    /// <summary>Malzeme (servis içi ilişki). Planda kullanılan malzeme silinemez.</summary>
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Material? Material { get; set; }
}
