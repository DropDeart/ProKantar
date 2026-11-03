using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.Machines;

/// <summary>Yeni bir Machine oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateMachineCommand : ICommand<Guid>
{
    /// <summary>Name.</summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    /// <summary>MachineType.</summary>
    [MaxLength(50)]
    public string MachineType { get; set; } = string.Empty;
    /// <summary>PlateOrSerial.</summary>
    [MaxLength(50)]
    public string? PlateOrSerial { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
}

internal sealed class CreateMachineHandler : ICommandHandler<CreateMachineCommand, Guid>
{
    private readonly IRepository<Machine> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateMachineHandler(IRepository<Machine> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateMachineCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Machine
        {
            Name = request.Name,
            MachineType = request.MachineType,
            PlateOrSerial = request.PlateOrSerial,
            IsActive = request.IsActive,
            StationId = request.StationId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Machine kaydını günceller.</summary>
public sealed class UpdateMachineCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    /// <summary>MachineType.</summary>
    [MaxLength(50)]
    public string MachineType { get; set; } = string.Empty;
    /// <summary>PlateOrSerial.</summary>
    [MaxLength(50)]
    public string? PlateOrSerial { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
}

internal sealed class UpdateMachineHandler : ICommandHandler<UpdateMachineCommand>
{
    private readonly IRepository<Machine> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateMachineHandler(IRepository<Machine> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateMachineCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Machine", request.Id);
        entity.Name = request.Name;
        entity.MachineType = request.MachineType;
        entity.PlateOrSerial = request.PlateOrSerial;
        entity.IsActive = request.IsActive;
        entity.StationId = request.StationId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Machine kaydını siler (soft delete).</summary>
public sealed class DeleteMachineCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteMachineHandler : ICommandHandler<DeleteMachineCommand>
{
    private readonly IRepository<Machine> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteMachineHandler(IRepository<Machine> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteMachineCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Machine", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
