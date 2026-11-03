using Kantar.Entities;

namespace Kantar.Features.WeighingAuditLogs;

/// <summary>WeighingAuditLog veri transfer nesnesi.</summary>
public sealed class WeighingAuditLogDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Action.</summary>
    public WeighingAuditLogAction Action { get; set; }
    /// <summary>FieldName.</summary>
    public string? FieldName { get; set; }
    /// <summary>OldValue.</summary>
    public string? OldValue { get; set; }
    /// <summary>NewValue.</summary>
    public string? NewValue { get; set; }
    /// <summary>Reason.</summary>
    public string? Reason { get; set; }
    /// <summary>WeighingId.</summary>
    public Guid WeighingId { get; set; }

    /// <summary>Bir WeighingAuditLog entity'sinden DTO üretir.</summary>
    public static WeighingAuditLogDto From(WeighingAuditLog entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new WeighingAuditLogDto
        {
            Id = entity.Id,
            Action = entity.Action,
            FieldName = entity.FieldName,
            OldValue = entity.OldValue,
            NewValue = entity.NewValue,
            Reason = entity.Reason,
            WeighingId = entity.WeighingId,
        };
    }
}
