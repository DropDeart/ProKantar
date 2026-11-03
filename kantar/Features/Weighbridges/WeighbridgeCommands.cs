using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.Weighbridges;

/// <summary>Yeni bir Weighbridge oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateWeighbridgeCommand : ICommand<Guid>
{
    /// <summary>Name.</summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    /// <summary>SerialNo.</summary>
    [MaxLength(50)]
    public string? SerialNo { get; set; }
    /// <summary>CapacityKg.</summary>
    public decimal CapacityKg { get; set; }
    /// <summary>ConnectionType.</summary>
    public WeighbridgeConnectionType? ConnectionType { get; set; }
    /// <summary>ConnectionInfo.</summary>
    public string? ConnectionInfo { get; set; }
    /// <summary>CalibrationValidUntil.</summary>
    public DateOnly? CalibrationValidUntil { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
}

internal sealed class CreateWeighbridgeHandler : ICommandHandler<CreateWeighbridgeCommand, Guid>
{
    private readonly IRepository<Weighbridge> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateWeighbridgeHandler(IRepository<Weighbridge> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateWeighbridgeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Weighbridge
        {
            Name = request.Name,
            SerialNo = request.SerialNo,
            CapacityKg = request.CapacityKg,
            ConnectionType = request.ConnectionType,
            ConnectionInfo = request.ConnectionInfo,
            CalibrationValidUntil = request.CalibrationValidUntil,
            IsActive = request.IsActive,
            StationId = request.StationId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Weighbridge kaydını günceller.</summary>
public sealed class UpdateWeighbridgeCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    /// <summary>SerialNo.</summary>
    [MaxLength(50)]
    public string? SerialNo { get; set; }
    /// <summary>CapacityKg.</summary>
    public decimal CapacityKg { get; set; }
    /// <summary>ConnectionType.</summary>
    public WeighbridgeConnectionType? ConnectionType { get; set; }
    /// <summary>ConnectionInfo.</summary>
    public string? ConnectionInfo { get; set; }
    /// <summary>CalibrationValidUntil.</summary>
    public DateOnly? CalibrationValidUntil { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
}

internal sealed class UpdateWeighbridgeHandler : ICommandHandler<UpdateWeighbridgeCommand>
{
    private readonly IRepository<Weighbridge> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateWeighbridgeHandler(IRepository<Weighbridge> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateWeighbridgeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Weighbridge", request.Id);
        entity.Name = request.Name;
        entity.SerialNo = request.SerialNo;
        entity.CapacityKg = request.CapacityKg;
        entity.ConnectionType = request.ConnectionType;
        entity.ConnectionInfo = request.ConnectionInfo;
        entity.CalibrationValidUntil = request.CalibrationValidUntil;
        entity.IsActive = request.IsActive;
        entity.StationId = request.StationId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Weighbridge kaydını siler (soft delete).</summary>
public sealed class DeleteWeighbridgeCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteWeighbridgeHandler : ICommandHandler<DeleteWeighbridgeCommand>
{
    private readonly IRepository<Weighbridge> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteWeighbridgeHandler(IRepository<Weighbridge> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteWeighbridgeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Weighbridge", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
