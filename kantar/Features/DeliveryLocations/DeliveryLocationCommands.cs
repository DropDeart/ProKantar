using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.DeliveryLocations;

/// <summary>Yeni bir DeliveryLocation oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateDeliveryLocationCommand : ICommand<Guid>
{
    /// <summary>Name.</summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Address.</summary>
    public string? Address { get; set; }
    /// <summary>Latitude.</summary>
    public decimal Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 150;
    /// <summary>ContactName.</summary>
    [MaxLength(100)]
    public string? ContactName { get; set; }
    /// <summary>ContactPhone.</summary>
    [MaxLength(20)]
    public string? ContactPhone { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>CompanyId.</summary>
    public Guid CompanyId { get; set; }
}

internal sealed class CreateDeliveryLocationHandler : ICommandHandler<CreateDeliveryLocationCommand, Guid>
{
    private readonly IRepository<DeliveryLocation> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateDeliveryLocationHandler(IRepository<DeliveryLocation> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateDeliveryLocationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new DeliveryLocation
        {
            Name = request.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusM = request.RadiusM,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            IsActive = request.IsActive,
            CompanyId = request.CompanyId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir DeliveryLocation kaydını günceller.</summary>
public sealed class UpdateDeliveryLocationCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Address.</summary>
    public string? Address { get; set; }
    /// <summary>Latitude.</summary>
    public decimal Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 150;
    /// <summary>ContactName.</summary>
    [MaxLength(100)]
    public string? ContactName { get; set; }
    /// <summary>ContactPhone.</summary>
    [MaxLength(20)]
    public string? ContactPhone { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>CompanyId.</summary>
    public Guid CompanyId { get; set; }
}

internal sealed class UpdateDeliveryLocationHandler : ICommandHandler<UpdateDeliveryLocationCommand>
{
    private readonly IRepository<DeliveryLocation> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateDeliveryLocationHandler(IRepository<DeliveryLocation> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateDeliveryLocationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("DeliveryLocation", request.Id);
        entity.Name = request.Name;
        entity.Address = request.Address;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.RadiusM = request.RadiusM;
        entity.ContactName = request.ContactName;
        entity.ContactPhone = request.ContactPhone;
        entity.IsActive = request.IsActive;
        entity.CompanyId = request.CompanyId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir DeliveryLocation kaydını siler (soft delete).</summary>
public sealed class DeleteDeliveryLocationCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteDeliveryLocationHandler : ICommandHandler<DeleteDeliveryLocationCommand>
{
    private readonly IRepository<DeliveryLocation> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteDeliveryLocationHandler(IRepository<DeliveryLocation> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteDeliveryLocationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("DeliveryLocation", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
