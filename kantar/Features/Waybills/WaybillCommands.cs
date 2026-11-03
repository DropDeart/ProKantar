using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using BaseForge.Core.Messaging;
using Kantar.Entities;

namespace Kantar.Features.Waybills;

/// <summary>Yeni bir Waybill oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateWaybillCommand : ICommand<Guid>
{
    /// <summary>WaybillNo.</summary>
    [MaxLength(30)]
    public string WaybillNo { get; set; } = string.Empty;
    /// <summary>WaybillType.</summary>
    public WaybillWaybillType WaybillType { get; set; }
    /// <summary>IssuedAt.</summary>
    public DateTimeOffset IssuedAt { get; set; }
    /// <summary>DeliveryAddress.</summary>
    public string? DeliveryAddress { get; set; }
    /// <summary>EDespatchUuid.</summary>
    public Guid? EDespatchUuid { get; set; }
    /// <summary>EDespatchStatus.</summary>
    public WaybillEDespatchStatus? EDespatchStatus { get; set; }
    /// <summary>ErpSyncStatus.</summary>
    public WaybillErpSyncStatus ErpSyncStatus { get; set; }
    /// <summary>ErpDocNo.</summary>
    [MaxLength(50)]
    public string? ErpDocNo { get; set; }
    /// <summary>ErpSyncedAt.</summary>
    public DateTimeOffset? ErpSyncedAt { get; set; }
    /// <summary>ErpError.</summary>
    public string? ErpError { get; set; }
    /// <summary>WeighingId.</summary>
    public Guid WeighingId { get; set; }
}

internal sealed class CreateWaybillHandler : ICommandHandler<CreateWaybillCommand, Guid>
{
    private readonly IRepository<Waybill> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public CreateWaybillHandler(IRepository<Waybill> repository, IUnitOfWork unitOfWork, IEventBus eventBus)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Guid> Handle(CreateWaybillCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Waybill
        {
            WaybillNo = request.WaybillNo,
            WaybillType = request.WaybillType,
            IssuedAt = request.IssuedAt,
            DeliveryAddress = request.DeliveryAddress,
            EDespatchUuid = request.EDespatchUuid,
            EDespatchStatus = request.EDespatchStatus,
            ErpSyncStatus = request.ErpSyncStatus,
            ErpDocNo = request.ErpDocNo,
            ErpSyncedAt = request.ErpSyncedAt,
            ErpError = request.ErpError,
            WeighingId = request.WeighingId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _eventBus.PublishAsync(new WaybillCreatedEvent { Data = WaybillDto.From(entity) }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Waybill kaydını günceller.</summary>
public sealed class UpdateWaybillCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>WaybillNo.</summary>
    [MaxLength(30)]
    public string WaybillNo { get; set; } = string.Empty;
    /// <summary>WaybillType.</summary>
    public WaybillWaybillType WaybillType { get; set; }
    /// <summary>IssuedAt.</summary>
    public DateTimeOffset IssuedAt { get; set; }
    /// <summary>DeliveryAddress.</summary>
    public string? DeliveryAddress { get; set; }
    /// <summary>EDespatchUuid.</summary>
    public Guid? EDespatchUuid { get; set; }
    /// <summary>EDespatchStatus.</summary>
    public WaybillEDespatchStatus? EDespatchStatus { get; set; }
    /// <summary>ErpSyncStatus.</summary>
    public WaybillErpSyncStatus ErpSyncStatus { get; set; }
    /// <summary>ErpDocNo.</summary>
    [MaxLength(50)]
    public string? ErpDocNo { get; set; }
    /// <summary>ErpSyncedAt.</summary>
    public DateTimeOffset? ErpSyncedAt { get; set; }
    /// <summary>ErpError.</summary>
    public string? ErpError { get; set; }
    /// <summary>WeighingId.</summary>
    public Guid WeighingId { get; set; }
}

internal sealed class UpdateWaybillHandler : ICommandHandler<UpdateWaybillCommand>
{
    private readonly IRepository<Waybill> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateWaybillHandler(IRepository<Waybill> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateWaybillCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Waybill", request.Id);
        entity.WaybillNo = request.WaybillNo;
        entity.WaybillType = request.WaybillType;
        entity.IssuedAt = request.IssuedAt;
        entity.DeliveryAddress = request.DeliveryAddress;
        entity.EDespatchUuid = request.EDespatchUuid;
        entity.EDespatchStatus = request.EDespatchStatus;
        entity.ErpSyncStatus = request.ErpSyncStatus;
        entity.ErpDocNo = request.ErpDocNo;
        entity.ErpSyncedAt = request.ErpSyncedAt;
        entity.ErpError = request.ErpError;
        entity.WeighingId = request.WeighingId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Waybill kaydını siler (soft delete).</summary>
public sealed class DeleteWaybillCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteWaybillHandler : ICommandHandler<DeleteWaybillCommand>
{
    private readonly IRepository<Waybill> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteWaybillHandler(IRepository<Waybill> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteWaybillCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Waybill", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
