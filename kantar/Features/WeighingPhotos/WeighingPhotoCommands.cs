using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.WeighingPhotos;

/// <summary>Yeni bir WeighingPhoto oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateWeighingPhotoCommand : ICommand<Guid>
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
}

internal sealed class CreateWeighingPhotoHandler : ICommandHandler<CreateWeighingPhotoCommand, Guid>
{
    private readonly IRepository<WeighingPhoto> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateWeighingPhotoHandler(IRepository<WeighingPhoto> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateWeighingPhotoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new WeighingPhoto
        {
            Stage = request.Stage,
            CameraName = request.CameraName,
            Url = request.Url,
            TakenAt = request.TakenAt,
            WeighingId = request.WeighingId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}


