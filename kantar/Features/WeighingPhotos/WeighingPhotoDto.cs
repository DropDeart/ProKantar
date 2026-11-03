using Kantar.Entities;

namespace Kantar.Features.WeighingPhotos;

/// <summary>WeighingPhoto veri transfer nesnesi.</summary>
public sealed class WeighingPhotoDto
{
    /// <summary>Kayıt kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Stage.</summary>
    public WeighingPhotoStage Stage { get; set; }
    /// <summary>CameraName.</summary>
    public string? CameraName { get; set; }
    /// <summary>Url.</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>TakenAt.</summary>
    public DateTimeOffset TakenAt { get; set; }
    /// <summary>WeighingId.</summary>
    public Guid WeighingId { get; set; }

    /// <summary>Bir WeighingPhoto entity'sinden DTO üretir.</summary>
    public static WeighingPhotoDto From(WeighingPhoto entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new WeighingPhotoDto
        {
            Id = entity.Id,
            Stage = entity.Stage,
            CameraName = entity.CameraName,
            Url = entity.Url,
            TakenAt = entity.TakenAt,
            WeighingId = entity.WeighingId,
        };
    }
}
