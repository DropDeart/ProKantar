using Platform.Entities;

namespace Platform.Features.Carriers;

/// <summary>Carrier veri transfer nesnesi.</summary>
public sealed class CarrierDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>TaxNo.</summary>
    public string? TaxNo { get; set; }
    /// <summary>TaxOffice.</summary>
    public string? TaxOffice { get; set; }
    /// <summary>Phone.</summary>
    public string? Phone { get; set; }
    /// <summary>Address.</summary>
    public string? Address { get; set; }

    /// <summary>Bir Carrier entity'sinden DTO üretir.</summary>
    public static CarrierDto From(Carrier entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new CarrierDto
        {
            Id = entity.Id,
            OwnerUserId = entity.OwnerUserId,
            Name = entity.Name,
            TaxNo = entity.TaxNo,
            TaxOffice = entity.TaxOffice,
            Phone = entity.Phone,
            Address = entity.Address,
        };
    }
}
