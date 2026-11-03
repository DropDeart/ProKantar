using Kantar.Entities;

namespace Kantar.Features.TenantVehicles;

/// <summary>TenantVehicle veri transfer nesnesi.</summary>
public sealed class TenantVehicleDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>StoredTareKg.</summary>
    public decimal? StoredTareKg { get; set; }
    /// <summary>StoredTareAt.</summary>
    public DateTimeOffset? StoredTareAt { get; set; }
    /// <summary>IsBlacklisted.</summary>
    public bool IsBlacklisted { get; set; } = false;
    /// <summary>BlacklistReason.</summary>
    public string? BlacklistReason { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>VehicleTypeId.</summary>
    public Guid? VehicleTypeId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Bir TenantVehicle entity'sinden DTO üretir.</summary>
    public static TenantVehicleDto From(TenantVehicle entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new TenantVehicleDto
        {
            Id = entity.Id,
            StoredTareKg = entity.StoredTareKg,
            StoredTareAt = entity.StoredTareAt,
            IsBlacklisted = entity.IsBlacklisted,
            BlacklistReason = entity.BlacklistReason,
            Note = entity.Note,
            VehicleTypeId = entity.VehicleTypeId,
            VehicleId = entity.VehicleId,
        };
    }
}
