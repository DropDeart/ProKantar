using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Platform.Entities;

namespace Platform.Features.Vehicles;

/// <summary>Yeni bir Vehicle oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateVehicleCommand : ICommand<Guid>
{
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>VehicleTypeCode.</summary>
    [MaxLength(30)]
    public string? VehicleTypeCode { get; set; }
    /// <summary>Brand.</summary>
    [MaxLength(50)]
    public string? Brand { get; set; }
    /// <summary>Model.</summary>
    [MaxLength(50)]
    public string? Model { get; set; }
    /// <summary>IsClaimed.</summary>
    public bool IsClaimed { get; set; } = false;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }
}

internal sealed class CreateVehicleHandler : ICommandHandler<CreateVehicleCommand, Guid>
{
    private readonly IRepository<Vehicle> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateVehicleHandler(IRepository<Vehicle> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Vehicle
        {
            OwnerUserId = request.OwnerUserId,
            Plate = request.Plate,
            VehicleTypeCode = request.VehicleTypeCode,
            Brand = request.Brand,
            Model = request.Model,
            IsClaimed = request.IsClaimed,
            CarrierId = request.CarrierId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Vehicle kaydını günceller.</summary>
public sealed class UpdateVehicleCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>VehicleTypeCode.</summary>
    [MaxLength(30)]
    public string? VehicleTypeCode { get; set; }
    /// <summary>Brand.</summary>
    [MaxLength(50)]
    public string? Brand { get; set; }
    /// <summary>Model.</summary>
    [MaxLength(50)]
    public string? Model { get; set; }
    /// <summary>IsClaimed.</summary>
    public bool IsClaimed { get; set; } = false;
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

internal sealed class UpdateVehicleHandler : ICommandHandler<UpdateVehicleCommand>
{
    private readonly IRepository<Vehicle> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateVehicleHandler(IRepository<Vehicle> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Vehicle", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.OwnerUserId != ownerId)
        {
            throw new ForbiddenException("Vehicle", request.Id);
        }

        entity.Plate = request.Plate;
        entity.VehicleTypeCode = request.VehicleTypeCode;
        entity.Brand = request.Brand;
        entity.Model = request.Model;
        entity.IsClaimed = request.IsClaimed;
        entity.CarrierId = request.CarrierId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Vehicle kaydını siler (soft delete).</summary>
public sealed class DeleteVehicleCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıt silinebilir (aksi halde 403). Controller doldurur.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class DeleteVehicleHandler : ICommandHandler<DeleteVehicleCommand>
{
    private readonly IRepository<Vehicle> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteVehicleHandler(IRepository<Vehicle> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Vehicle", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.OwnerUserId != ownerId)
        {
            throw new ForbiddenException("Vehicle", request.Id);
        }
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
