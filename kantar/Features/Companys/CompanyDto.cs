using Kantar.Entities;

namespace Kantar.Features.Companys;

/// <summary>Company veri transfer nesnesi.</summary>
public sealed class CompanyDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>ShortName.</summary>
    public string? ShortName { get; set; }
    /// <summary>TaxNo.</summary>
    public string? TaxNo { get; set; }
    /// <summary>TaxOffice.</summary>
    public string? TaxOffice { get; set; }
    /// <summary>IsCustomer.</summary>
    public bool IsCustomer { get; set; } = false;
    /// <summary>IsSupplier.</summary>
    public bool IsSupplier { get; set; } = false;
    /// <summary>IsCarrier.</summary>
    public bool IsCarrier { get; set; } = false;
    /// <summary>Phone.</summary>
    public string? Phone { get; set; }
    /// <summary>Email.</summary>
    public string? Email { get; set; }
    /// <summary>Address.</summary>
    public string? Address { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Bir Company entity'sinden DTO üretir.</summary>
    public static CompanyDto From(Company entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new CompanyDto
        {
            Id = entity.Id,
            Name = entity.Name,
            ShortName = entity.ShortName,
            TaxNo = entity.TaxNo,
            TaxOffice = entity.TaxOffice,
            IsCustomer = entity.IsCustomer,
            IsSupplier = entity.IsSupplier,
            IsCarrier = entity.IsCarrier,
            Phone = entity.Phone,
            Email = entity.Email,
            Address = entity.Address,
            IsActive = entity.IsActive,
        };
    }
}
