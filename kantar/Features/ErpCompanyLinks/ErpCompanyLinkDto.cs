using Kantar.Entities;

namespace Kantar.Features.ErpCompanyLinks;

/// <summary>ErpCompanyLink veri transfer nesnesi.</summary>
public sealed class ErpCompanyLinkDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>ErpSystem.</summary>
    public string ErpSystem { get; set; } = string.Empty;
    /// <summary>ErpCode.</summary>
    public string ErpCode { get; set; } = string.Empty;
    /// <summary>IsDefault.</summary>
    public bool IsDefault { get; set; } = true;
    /// <summary>CompanyId.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Bir ErpCompanyLink entity'sinden DTO üretir.</summary>
    public static ErpCompanyLinkDto From(ErpCompanyLink entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new ErpCompanyLinkDto
        {
            Id = entity.Id,
            ErpSystem = entity.ErpSystem,
            ErpCode = entity.ErpCode,
            IsDefault = entity.IsDefault,
            CompanyId = entity.CompanyId,
        };
    }
}
