using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>WeighingPhoto entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class WeighingPhoto : BaseEntity, ITenantEntity
{
    /// <summary>Stage.</summary>
    public WeighingPhotoStage Stage { get; set; }
    /// <summary>CameraName.</summary>
    [MaxLength(50)]
    public string? CameraName { get; set; }
    /// <summary>Url.</summary>
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;
    /// <summary>TakenAt.</summary>
    public DateTimeOffset TakenAt { get; set; }
    /// <summary>WeighingId.</summary>
    public Guid WeighingId { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Weighing (servis içi ilişki).</summary>
    public Weighing? Weighing { get; set; }
}
