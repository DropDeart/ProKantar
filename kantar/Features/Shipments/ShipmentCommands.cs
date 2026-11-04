using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.Shipments;

/// <summary>Yeni bir Shipment oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateShipmentCommand : ICommand<Guid>
{
    /// <summary>ShipmentNo.</summary>
    [MaxLength(30)]
    public string ShipmentNo { get; set; } = string.Empty;
    /// <summary>Direction.</summary>
    public ShipmentDirection Direction { get; set; }
    /// <summary>PlannedAt.</summary>
    public DateTimeOffset PlannedAt { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>DriverId.</summary>
    public Guid? DriverId { get; set; }
    /// <summary>DriverName.</summary>
    [MaxLength(150)]
    public string? DriverName { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
}

internal sealed class CreateShipmentHandler : ICommandHandler<CreateShipmentCommand, Guid>
{
    private readonly IRepository<Shipment> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateShipmentHandler(IRepository<Shipment> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Shipment
        {
            ShipmentNo = request.ShipmentNo,
            Direction = request.Direction,
            PlannedAt = request.PlannedAt,
            Plate = request.Plate,
            DriverId = request.DriverId,
            DriverName = request.DriverName,
            Note = request.Note,
            StationId = request.StationId,
            VehicleId = request.VehicleId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Shipment kaydını günceller.</summary>
public sealed class UpdateShipmentCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>ShipmentNo.</summary>
    [MaxLength(30)]
    public string ShipmentNo { get; set; } = string.Empty;
    /// <summary>Direction.</summary>
    public ShipmentDirection Direction { get; set; }
    /// <summary>PlannedAt.</summary>
    public DateTimeOffset PlannedAt { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>DriverId.</summary>
    public Guid? DriverId { get; set; }
    /// <summary>DriverName.</summary>
    [MaxLength(150)]
    public string? DriverName { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
}

internal sealed class UpdateShipmentHandler : ICommandHandler<UpdateShipmentCommand>
{
    private readonly IRepository<Shipment> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateShipmentHandler(IRepository<Shipment> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateShipmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Shipment", request.Id);
        entity.ShipmentNo = request.ShipmentNo;
        entity.Direction = request.Direction;
        entity.PlannedAt = request.PlannedAt;
        entity.Plate = request.Plate;
        entity.DriverId = request.DriverId;
        entity.DriverName = request.DriverName;
        entity.Note = request.Note;
        entity.StationId = request.StationId;
        entity.VehicleId = request.VehicleId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Shipment kaydını siler (soft delete).</summary>
public sealed class DeleteShipmentCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteShipmentHandler : ICommandHandler<DeleteShipmentCommand>
{
    private readonly IRepository<Shipment> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteShipmentHandler(IRepository<Shipment> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteShipmentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Shipment", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
