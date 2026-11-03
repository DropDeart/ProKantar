using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Platform.Entities;

namespace Platform.Features.Trailers;

/// <summary>Yeni bir Trailer oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateTrailerCommand : ICommand<Guid>
{
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }
}

internal sealed class CreateTrailerHandler : ICommandHandler<CreateTrailerCommand, Guid>
{
    private readonly IRepository<Trailer> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateTrailerHandler(IRepository<Trailer> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateTrailerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Trailer
        {
            OwnerUserId = request.OwnerUserId,
            Plate = request.Plate,
            CarrierId = request.CarrierId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Trailer kaydını günceller.</summary>
public sealed class UpdateTrailerCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }

    /// <summary>
    /// Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıt güncellenebilir (aksi halde 403). İstemciden
    /// bağlanmaz; controller rol/sahiplik kuralına göre doldurur (bkz. docs/ARCH.md §6.1).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class UpdateTrailerHandler : ICommandHandler<UpdateTrailerCommand>
{
    private readonly IRepository<Trailer> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateTrailerHandler(IRepository<Trailer> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateTrailerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Trailer", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.OwnerUserId != ownerId)
        {
            throw new ForbiddenException("Trailer", request.Id);
        }

        entity.Plate = request.Plate;
        entity.CarrierId = request.CarrierId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Trailer kaydını siler (soft delete).</summary>
public sealed class DeleteTrailerCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıt silinebilir (aksi halde 403). Controller doldurur.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class DeleteTrailerHandler : ICommandHandler<DeleteTrailerCommand>
{
    private readonly IRepository<Trailer> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteTrailerHandler(IRepository<Trailer> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteTrailerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Trailer", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.OwnerUserId != ownerId)
        {
            throw new ForbiddenException("Trailer", request.Id);
        }
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
