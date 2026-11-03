using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Platform.Entities;

namespace Platform.Features.DriverVehicles;

/// <summary>Yeni bir DriverVehicle oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateDriverVehicleCommand : ICommand<Guid>
{
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>StartedAt.</summary>
    public DateTimeOffset StartedAt { get; set; }
    /// <summary>EndedAt.</summary>
    public DateTimeOffset? EndedAt { get; set; }
    /// <summary>DriverId.</summary>
    public Guid DriverId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
    /// <summary>TrailerId.</summary>
    public Guid? TrailerId { get; set; }
}

internal sealed class CreateDriverVehicleHandler : ICommandHandler<CreateDriverVehicleCommand, Guid>
{
    private readonly IRepository<DriverVehicle> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateDriverVehicleHandler(IRepository<DriverVehicle> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateDriverVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new DriverVehicle
        {
            UserId = request.UserId,
            StartedAt = request.StartedAt,
            EndedAt = request.EndedAt,
            DriverId = request.DriverId,
            VehicleId = request.VehicleId,
            TrailerId = request.TrailerId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir DriverVehicle kaydını günceller.</summary>
public sealed class UpdateDriverVehicleCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>StartedAt.</summary>
    public DateTimeOffset StartedAt { get; set; }
    /// <summary>EndedAt.</summary>
    public DateTimeOffset? EndedAt { get; set; }
    /// <summary>DriverId.</summary>
    public Guid DriverId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
    /// <summary>TrailerId.</summary>
    public Guid? TrailerId { get; set; }

    /// <summary>
    /// Doluysa yalnızca UserId'ı bu kullanıcı olan kayıt güncellenebilir (aksi halde 403). İstemciden
    /// bağlanmaz; controller rol/sahiplik kuralına göre doldurur (bkz. docs/ARCH.md §6.1).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class UpdateDriverVehicleHandler : ICommandHandler<UpdateDriverVehicleCommand>
{
    private readonly IRepository<DriverVehicle> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateDriverVehicleHandler(IRepository<DriverVehicle> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateDriverVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("DriverVehicle", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.UserId != ownerId)
        {
            throw new ForbiddenException("DriverVehicle", request.Id);
        }

        entity.StartedAt = request.StartedAt;
        entity.EndedAt = request.EndedAt;
        entity.DriverId = request.DriverId;
        entity.VehicleId = request.VehicleId;
        entity.TrailerId = request.TrailerId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir DriverVehicle kaydını siler (soft delete).</summary>
public sealed class DeleteDriverVehicleCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>Doluysa yalnızca UserId'ı bu kullanıcı olan kayıt silinebilir (aksi halde 403). Controller doldurur.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class DeleteDriverVehicleHandler : ICommandHandler<DeleteDriverVehicleCommand>
{
    private readonly IRepository<DriverVehicle> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteDriverVehicleHandler(IRepository<DriverVehicle> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteDriverVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("DriverVehicle", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.UserId != ownerId)
        {
            throw new ForbiddenException("DriverVehicle", request.Id);
        }
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
