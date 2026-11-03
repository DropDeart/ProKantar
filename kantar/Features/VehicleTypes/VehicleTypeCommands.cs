using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.VehicleTypes;

/// <summary>Yeni bir VehicleType oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateVehicleTypeCommand : ICommand<Guid>
{
    /// <summary>Code.</summary>
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;
    /// <summary>Name.</summary>
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    /// <summary>MaxGrossKg.</summary>
    public decimal? MaxGrossKg { get; set; }
}

internal sealed class CreateVehicleTypeHandler : ICommandHandler<CreateVehicleTypeCommand, Guid>
{
    private readonly IRepository<VehicleType> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateVehicleTypeHandler(IRepository<VehicleType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateVehicleTypeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new VehicleType
        {
            Code = request.Code,
            Name = request.Name,
            MaxGrossKg = request.MaxGrossKg,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir VehicleType kaydını günceller.</summary>
public sealed class UpdateVehicleTypeCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Code.</summary>
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;
    /// <summary>Name.</summary>
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    /// <summary>MaxGrossKg.</summary>
    public decimal? MaxGrossKg { get; set; }
}

internal sealed class UpdateVehicleTypeHandler : ICommandHandler<UpdateVehicleTypeCommand>
{
    private readonly IRepository<VehicleType> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateVehicleTypeHandler(IRepository<VehicleType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateVehicleTypeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("VehicleType", request.Id);
        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.MaxGrossKg = request.MaxGrossKg;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir VehicleType kaydını siler (soft delete).</summary>
public sealed class DeleteVehicleTypeCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteVehicleTypeHandler : ICommandHandler<DeleteVehicleTypeCommand>
{
    private readonly IRepository<VehicleType> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteVehicleTypeHandler(IRepository<VehicleType> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteVehicleTypeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("VehicleType", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
