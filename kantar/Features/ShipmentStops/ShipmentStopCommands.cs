using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.ShipmentStops;

/// <summary>Yeni bir ShipmentStop oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateShipmentStopCommand : ICommand<Guid>
{
    /// <summary>Sequence.</summary>
    public int Sequence { get; set; }
    /// <summary>StopType.</summary>
    public ShipmentStopStopType StopType { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Latitude.</summary>
    public decimal Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 150;
    /// <summary>PlannedAt.</summary>
    public DateTimeOffset? PlannedAt { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>ShipmentId.</summary>
    public Guid ShipmentId { get; set; }
    /// <summary>DeliveryLocationId.</summary>
    public Guid? DeliveryLocationId { get; set; }
    /// <summary>StationId.</summary>
    public Guid? StationId { get; set; }
    /// <summary>WaybillId.</summary>
    public Guid? WaybillId { get; set; }
}

internal sealed class CreateShipmentStopHandler : ICommandHandler<CreateShipmentStopCommand, Guid>
{
    private readonly IRepository<ShipmentStop> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateShipmentStopHandler(IRepository<ShipmentStop> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateShipmentStopCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new ShipmentStop
        {
            Sequence = request.Sequence,
            StopType = request.StopType,
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusM = request.RadiusM,
            PlannedAt = request.PlannedAt,
            Note = request.Note,
            ShipmentId = request.ShipmentId,
            DeliveryLocationId = request.DeliveryLocationId,
            StationId = request.StationId,
            WaybillId = request.WaybillId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir ShipmentStop kaydını günceller.</summary>
public sealed class UpdateShipmentStopCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Sequence.</summary>
    public int Sequence { get; set; }
    /// <summary>StopType.</summary>
    public ShipmentStopStopType StopType { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Latitude.</summary>
    public decimal Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 150;
    /// <summary>PlannedAt.</summary>
    public DateTimeOffset? PlannedAt { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>ShipmentId.</summary>
    public Guid ShipmentId { get; set; }
    /// <summary>DeliveryLocationId.</summary>
    public Guid? DeliveryLocationId { get; set; }
    /// <summary>StationId.</summary>
    public Guid? StationId { get; set; }
    /// <summary>WaybillId.</summary>
    public Guid? WaybillId { get; set; }
}

internal sealed class UpdateShipmentStopHandler : ICommandHandler<UpdateShipmentStopCommand>
{
    private readonly IRepository<ShipmentStop> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateShipmentStopHandler(IRepository<ShipmentStop> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateShipmentStopCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("ShipmentStop", request.Id);
        entity.Sequence = request.Sequence;
        entity.StopType = request.StopType;
        entity.Name = request.Name;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.RadiusM = request.RadiusM;
        entity.PlannedAt = request.PlannedAt;
        entity.Note = request.Note;
        entity.ShipmentId = request.ShipmentId;
        entity.DeliveryLocationId = request.DeliveryLocationId;
        entity.StationId = request.StationId;
        entity.WaybillId = request.WaybillId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir ShipmentStop kaydını siler (soft delete).</summary>
public sealed class DeleteShipmentStopCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteShipmentStopHandler : ICommandHandler<DeleteShipmentStopCommand>
{
    private readonly IRepository<ShipmentStop> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteShipmentStopHandler(IRepository<ShipmentStop> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteShipmentStopCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("ShipmentStop", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
