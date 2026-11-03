using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Kantar.Entities;

/// <summary>WeighingAuditLog entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class WeighingAuditLog : BaseEntity, ITenantEntity
{
    /// <summary>Action.</summary>
    public WeighingAuditLogAction Action { get; set; }
    /// <summary>FieldName.</summary>
    [MaxLength(50)]
    public string? FieldName { get; set; }
    /// <summary>OldValue.</summary>
    [MaxLength(250)]
    public string? OldValue { get; set; }
    /// <summary>NewValue.</summary>
    [MaxLength(250)]
    public string? NewValue { get; set; }
    /// <summary>Reason.</summary>
    [MaxLength(250)]
    public string? Reason { get; set; }
    /// <summary>WeighingId.</summary>
    public Guid WeighingId { get; set; }
    /// <summary>TenantId.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Weighing (servis içi ilişki).</summary>
    public Weighing? Weighing { get; set; }
}
