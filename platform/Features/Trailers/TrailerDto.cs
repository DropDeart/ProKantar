using Platform.Entities;

namespace Platform.Features.Trailers;

/// <summary>Trailer veri transfer nesnesi.</summary>
public sealed class TrailerDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Plate.</summary>
    public string Plate { get; set; } = string.Empty;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }

    /// <summary>Bir Trailer entity'sinden DTO üretir.</summary>
    public static TrailerDto From(Trailer entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new TrailerDto
        {
            Id = entity.Id,
            OwnerUserId = entity.OwnerUserId,
            Plate = entity.Plate,
            CarrierId = entity.CarrierId,
        };
    }
}
