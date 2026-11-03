using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.TenantVehicles;

/// <summary>Yeni bir TenantVehicle oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateTenantVehicleCommand : ICommand<Guid>
{
    /// <summary>StoredTareKg.</summary>
    public decimal? StoredTareKg { get; set; }
    /// <summary>StoredTareAt.</summary>
    public DateTimeOffset? StoredTareAt { get; set; }
    /// <summary>IsBlacklisted.</summary>
    public bool IsBlacklisted { get; set; } = false;
    /// <summary>BlacklistReason.</summary>
    [MaxLength(250)]
    public string? BlacklistReason { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>VehicleTypeId.</summary>
    public Guid? VehicleTypeId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
}

internal sealed class CreateTenantVehicleHandler : ICommandHandler<CreateTenantVehicleCommand, Guid>
{
    private readonly IRepository<TenantVehicle> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateTenantVehicleHandler(IRepository<TenantVehicle> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateTenantVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new TenantVehicle
        {
            StoredTareKg = request.StoredTareKg,
            StoredTareAt = request.StoredTareAt,
            IsBlacklisted = request.IsBlacklisted,
            BlacklistReason = request.BlacklistReason,
            Note = request.Note,
            VehicleTypeId = request.VehicleTypeId,
            VehicleId = request.VehicleId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir TenantVehicle kaydını günceller.</summary>
public sealed class UpdateTenantVehicleCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>StoredTareKg.</summary>
    public decimal? StoredTareKg { get; set; }
    /// <summary>StoredTareAt.</summary>
    public DateTimeOffset? StoredTareAt { get; set; }
    /// <summary>IsBlacklisted.</summary>
    public bool IsBlacklisted { get; set; } = false;
    /// <summary>BlacklistReason.</summary>
    [MaxLength(250)]
    public string? BlacklistReason { get; set; }
    /// <summary>Note.</summary>
    public string? Note { get; set; }
    /// <summary>VehicleTypeId.</summary>
    public Guid? VehicleTypeId { get; set; }
    /// <summary>VehicleId.</summary>
    public Guid VehicleId { get; set; }
}

internal sealed class UpdateTenantVehicleHandler : ICommandHandler<UpdateTenantVehicleCommand>
{
    private readonly IRepository<TenantVehicle> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateTenantVehicleHandler(IRepository<TenantVehicle> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateTenantVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("TenantVehicle", request.Id);
        entity.StoredTareKg = request.StoredTareKg;
        entity.StoredTareAt = request.StoredTareAt;
        entity.IsBlacklisted = request.IsBlacklisted;
        entity.BlacklistReason = request.BlacklistReason;
        entity.Note = request.Note;
        entity.VehicleTypeId = request.VehicleTypeId;
        entity.VehicleId = request.VehicleId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir TenantVehicle kaydını siler (soft delete).</summary>
public sealed class DeleteTenantVehicleCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteTenantVehicleHandler : ICommandHandler<DeleteTenantVehicleCommand>
{
    private readonly IRepository<TenantVehicle> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteTenantVehicleHandler(IRepository<TenantVehicle> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteTenantVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("TenantVehicle", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
