using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using BaseForge.Core.Messaging;
using Kantar.Entities;

namespace Kantar.Features.Weighings;

/// <summary>Yeni bir Weighing oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateWeighingCommand : ICommand<Guid>
{
    /// <summary>TicketNo.</summary>
    [MaxLength(30)]
    public string TicketNo { get; set; } = string.Empty;
    /// <summary>Direction.</summary>
    public WeighingDirection Direction { get; set; }
    /// <summary>Status.</summary>
    public WeighingStatus Status { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>TrailerId.</summary>
    public Guid? TrailerId { get; set; }
    /// <summary>TrailerPlate.</summary>
    [MaxLength(15)]
    public string? TrailerPlate { get; set; }
    /// <summary>DriverId.</summary>
    public Guid? DriverId { get; set; }
    /// <summary>DriverName.</summary>
    [MaxLength(150)]
    public string? DriverName { get; set; }
    /// <summary>DriverPhone.</summary>
    [MaxLength(20)]
    public string? DriverPhone { get; set; }
    /// <summary>FirstWeightKg.</summary>
    public decimal FirstWeightKg { get; set; }
    /// <summary>FirstWeighedAt.</summary>
    public DateTimeOffset FirstWeighedAt { get; set; }
    /// <summary>FirstIsManual.</summary>
    public bool FirstIsManual { get; set; } = false;
    /// <summary>SecondWeighbridgeId.</summary>
    public Guid? SecondWeighbridgeId { get; set; }
    /// <summary>SecondWeightKg.</summary>
    public decimal? SecondWeightKg { get; set; }
    /// <summary>SecondWeighedAt.</summary>
    public DateTimeOffset? SecondWeighedAt { get; set; }
    /// <summary>SecondIsManual.</summary>
    public bool SecondIsManual { get; set; } = false;
    /// <summary>GrossKg.</summary>
    public decimal? GrossKg { get; set; }
    /// <summary>TareKg.</summary>
    public decimal? TareKg { get; set; }
    /// <summary>NetKg.</summary>
    public decimal? NetKg { get; set; }
    /// <summary>TareSource.</summary>
    public WeighingTareSource? TareSource { get; set; }
    /// <summary>CancelReason.</summary>
    [MaxLength(250)]
    public string? CancelReason { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
    /// <summary>WeighbridgeId.</summary>
    public Guid WeighbridgeId { get; set; }
    /// <summary>CompanyId.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>MaterialId.</summary>
    public Guid MaterialId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
}

internal sealed class CreateWeighingHandler : ICommandHandler<CreateWeighingCommand, Guid>
{
    private readonly IRepository<Weighing> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public CreateWeighingHandler(IRepository<Weighing> repository, IUnitOfWork unitOfWork, IEventBus eventBus)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Guid> Handle(CreateWeighingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Weighing
        {
            TicketNo = request.TicketNo,
            Direction = request.Direction,
            Status = request.Status,
            Plate = request.Plate,
            TrailerId = request.TrailerId,
            TrailerPlate = request.TrailerPlate,
            DriverId = request.DriverId,
            DriverName = request.DriverName,
            DriverPhone = request.DriverPhone,
            FirstWeightKg = request.FirstWeightKg,
            FirstWeighedAt = request.FirstWeighedAt,
            FirstIsManual = request.FirstIsManual,
            SecondWeighbridgeId = request.SecondWeighbridgeId,
            SecondWeightKg = request.SecondWeightKg,
            SecondWeighedAt = request.SecondWeighedAt,
            SecondIsManual = request.SecondIsManual,
            GrossKg = request.GrossKg,
            TareKg = request.TareKg,
            NetKg = request.NetKg,
            TareSource = request.TareSource,
            CancelReason = request.CancelReason,
            Note = request.Note,
            StationId = request.StationId,
            WeighbridgeId = request.WeighbridgeId,
            CompanyId = request.CompanyId,
            MaterialId = request.MaterialId,
            VehicleId = request.VehicleId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _eventBus.PublishAsync(new WeighingCreatedEvent { Data = WeighingDto.From(entity) }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Weighing kaydını günceller.</summary>
public sealed class UpdateWeighingCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>TicketNo.</summary>
    [MaxLength(30)]
    public string TicketNo { get; set; } = string.Empty;
    /// <summary>Direction.</summary>
    public WeighingDirection Direction { get; set; }
    /// <summary>Status.</summary>
    public WeighingStatus Status { get; set; }
    /// <summary>Plate.</summary>
    [MaxLength(15)]
    public string Plate { get; set; } = string.Empty;
    /// <summary>TrailerId.</summary>
    public Guid? TrailerId { get; set; }
    /// <summary>TrailerPlate.</summary>
    [MaxLength(15)]
    public string? TrailerPlate { get; set; }
    /// <summary>DriverId.</summary>
    public Guid? DriverId { get; set; }
    /// <summary>DriverName.</summary>
    [MaxLength(150)]
    public string? DriverName { get; set; }
    /// <summary>DriverPhone.</summary>
    [MaxLength(20)]
    public string? DriverPhone { get; set; }
    /// <summary>FirstWeightKg.</summary>
    public decimal FirstWeightKg { get; set; }
    /// <summary>FirstWeighedAt.</summary>
    public DateTimeOffset FirstWeighedAt { get; set; }
    /// <summary>FirstIsManual.</summary>
    public bool FirstIsManual { get; set; } = false;
    /// <summary>SecondWeighbridgeId.</summary>
    public Guid? SecondWeighbridgeId { get; set; }
    /// <summary>SecondWeightKg.</summary>
    public decimal? SecondWeightKg { get; set; }
    /// <summary>SecondWeighedAt.</summary>
    public DateTimeOffset? SecondWeighedAt { get; set; }
    /// <summary>SecondIsManual.</summary>
    public bool SecondIsManual { get; set; } = false;
    /// <summary>GrossKg.</summary>
    public decimal? GrossKg { get; set; }
    /// <summary>TareKg.</summary>
    public decimal? TareKg { get; set; }
    /// <summary>NetKg.</summary>
    public decimal? NetKg { get; set; }
    /// <summary>TareSource.</summary>
    public WeighingTareSource? TareSource { get; set; }
    /// <summary>CancelReason.</summary>
    [MaxLength(250)]
    public string? CancelReason { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>StationId.</summary>
    public Guid StationId { get; set; }
    /// <summary>WeighbridgeId.</summary>
    public Guid WeighbridgeId { get; set; }
    /// <summary>CompanyId.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>MaterialId.</summary>
    public Guid MaterialId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
}

internal sealed class UpdateWeighingHandler : ICommandHandler<UpdateWeighingCommand>
{
    private readonly IRepository<Weighing> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public UpdateWeighingHandler(IRepository<Weighing> repository, IUnitOfWork unitOfWork, IEventBus eventBus)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task Handle(UpdateWeighingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Weighing", request.Id);
        entity.TicketNo = request.TicketNo;
        entity.Direction = request.Direction;
        entity.Status = request.Status;
        entity.Plate = request.Plate;
        entity.TrailerId = request.TrailerId;
        entity.TrailerPlate = request.TrailerPlate;
        entity.DriverId = request.DriverId;
        entity.DriverName = request.DriverName;
        entity.DriverPhone = request.DriverPhone;
        entity.FirstWeightKg = request.FirstWeightKg;
        entity.FirstWeighedAt = request.FirstWeighedAt;
        entity.FirstIsManual = request.FirstIsManual;
        entity.SecondWeighbridgeId = request.SecondWeighbridgeId;
        entity.SecondWeightKg = request.SecondWeightKg;
        entity.SecondWeighedAt = request.SecondWeighedAt;
        entity.SecondIsManual = request.SecondIsManual;
        entity.GrossKg = request.GrossKg;
        entity.TareKg = request.TareKg;
        entity.NetKg = request.NetKg;
        entity.TareSource = request.TareSource;
        entity.CancelReason = request.CancelReason;
        entity.Note = request.Note;
        entity.StationId = request.StationId;
        entity.WeighbridgeId = request.WeighbridgeId;
        entity.CompanyId = request.CompanyId;
        entity.MaterialId = request.MaterialId;
        entity.VehicleId = request.VehicleId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _eventBus.PublishAsync(new WeighingUpdatedEvent { Data = WeighingDto.From(entity) }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Weighing kaydını siler (soft delete).</summary>
public sealed class DeleteWeighingCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteWeighingHandler : ICommandHandler<DeleteWeighingCommand>
{
    private readonly IRepository<Weighing> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteWeighingHandler(IRepository<Weighing> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteWeighingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Weighing", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
