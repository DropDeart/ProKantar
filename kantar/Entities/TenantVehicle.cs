using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>TenantVehicle entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class TenantVehicle : BaseEntity, ITenantEntity
{
    /// <summary>StoredTareKg.</summary>
    public decimal? StoredTareKg { get; set; }
    /// <summary>StoredTareAt.</summary>
    public DateTimeOffset? StoredTareAt { get; set; }
    /// <summary>IsBlacklisted.</summary>
    public bool IsBlacklisted { get; set; } = false;
    /// <summary>BlacklistReason.</summary>
    [MaxLength(250)]
    public string? BlacklistReason { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>VehicleTypeId.</summary>
    public Guid? VehicleTypeId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>VehicleType (servis içi ilişki).</summary>
    public VehicleType? VehicleType { get; set; }
}
