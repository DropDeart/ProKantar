using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.Stations;

/// <summary>Yeni bir Station oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateStationCommand : ICommand<Guid>
{
    /// <summary>Name.</summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Code.</summary>
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;
    /// <summary>Address.</summary>
    public string? Address { get; set; }
    /// <summary>İl.</summary>
    [MaxLength(50)]
    public string? City { get; set; }
    /// <summary>İlçe.</summary>
    [MaxLength(50)]
    public string? District { get; set; }
    /// <summary>Posta kodu (5 hane).</summary>
    [MaxLength(5)]
    public string? PostalCode { get; set; }
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }
    /// <summary>Latitude.</summary>
    public decimal? Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal? Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 300;
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
}

internal sealed class CreateStationHandler : ICommandHandler<CreateStationCommand, Guid>
{
    private readonly IRepository<Station> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateStationHandler(IRepository<Station> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateStationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Station
        {
            Name = request.Name,
            Code = request.Code,
            Address = request.Address,
            City = request.City,
            District = request.District,
            PostalCode = request.PostalCode,
            Phone = request.Phone,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusM = request.RadiusM,
            IsActive = request.IsActive,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Station kaydını günceller.</summary>
public sealed class UpdateStationCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Code.</summary>
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;
    /// <summary>Address.</summary>
    public string? Address { get; set; }
    /// <summary>İl.</summary>
    [MaxLength(50)]
    public string? City { get; set; }
    /// <summary>İlçe.</summary>
    [MaxLength(50)]
    public string? District { get; set; }
    /// <summary>Posta kodu (5 hane).</summary>
    [MaxLength(5)]
    public string? PostalCode { get; set; }
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }
    /// <summary>Latitude.</summary>
    public decimal? Latitude { get; set; }
    /// <summary>Longitude.</summary>
    public decimal? Longitude { get; set; }
    /// <summary>RadiusM.</summary>
    public int RadiusM { get; set; } = 300;
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
}

internal sealed class UpdateStationHandler : ICommandHandler<UpdateStationCommand>
{
    private readonly IRepository<Station> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateStationHandler(IRepository<Station> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateStationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Station", request.Id);
        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.Address = request.Address;
        entity.City = request.City;
        entity.District = request.District;
        entity.PostalCode = request.PostalCode;
        entity.Phone = request.Phone;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.RadiusM = request.RadiusM;
        entity.IsActive = request.IsActive;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Station kaydını siler (soft delete).</summary>
public sealed class DeleteStationCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteStationHandler : ICommandHandler<DeleteStationCommand>
{
    private readonly IRepository<Station> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteStationHandler(IRepository<Station> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteStationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Station", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
