using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BaseForge.Core.Entities;

namespace Platform.Entities;

/// <summary>Trailer entity'si (BaseForge.CodeGen tarafından üretildi).</summary>
public sealed class Trailer : BaseEntity
{
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }
    /// <summary>Carrier (servis içi ilişki).</summary>
    public Carrier? Carrier { get; set; }
}
