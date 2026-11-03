using Kantar.Entities;

namespace Kantar.Features.Materials;

/// <summary>Material veri transfer nesnesi.</summary>
public sealed class MaterialDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Code.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Unit.</summary>
    public MaterialUnit Unit { get; set; }
    /// <summary>KgPerUnit.</summary>
    public decimal? KgPerUnit { get; set; }
    /// <summary>ErpStockCode.</summary>
    public string? ErpStockCode { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Bir Material entity'sinden DTO üretir.</summary>
    public static MaterialDto From(Material entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new MaterialDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Unit = entity.Unit,
            KgPerUnit = entity.KgPerUnit,
            ErpStockCode = entity.ErpStockCode,
            IsActive = entity.IsActive,
        };
    }
}
