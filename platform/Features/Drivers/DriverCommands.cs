using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Platform.Entities;

namespace Platform.Features.Drivers;

/// <summary>Yeni bir Driver oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateDriverCommand : ICommand<Guid>
{
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>FullName.</summary>
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    /// <summary>NationalId.</summary>
    [MaxLength(11)]
    public string? NationalId { get; set; }
    /// <summary>LicenseNo.</summary>
    [MaxLength(30)]
    public string? LicenseNo { get; set; }
    /// <summary>KvkkConsentAt.</summary>
    public DateTimeOffset? KvkkConsentAt { get; set; }
    /// <summary>LocationConsent.</summary>
    public bool LocationConsent { get; set; } = false;
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }
}

internal sealed class CreateDriverHandler : ICommandHandler<CreateDriverCommand, Guid>
{
    private readonly IRepository<Driver> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateDriverHandler(IRepository<Driver> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateDriverCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Driver
        {
            UserId = request.UserId,
            FullName = request.FullName,
            Phone = request.Phone,
            NationalId = request.NationalId,
            LicenseNo = request.LicenseNo,
            KvkkConsentAt = request.KvkkConsentAt,
            LocationConsent = request.LocationConsent,
            IsActive = request.IsActive,
            CarrierId = request.CarrierId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Driver kaydını günceller.</summary>
public sealed class UpdateDriverCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>FullName.</summary>
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    /// <summary>NationalId.</summary>
    [MaxLength(11)]
    public string? NationalId { get; set; }
    /// <summary>LicenseNo.</summary>
    [MaxLength(30)]
    public string? LicenseNo { get; set; }
    /// <summary>KvkkConsentAt.</summary>
    public DateTimeOffset? KvkkConsentAt { get; set; }
    /// <summary>LocationConsent.</summary>
    public bool LocationConsent { get; set; } = false;
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>CarrierId.</summary>
    public Guid? CarrierId { get; set; }

    /// <summary>
    /// Doluysa yalnızca UserId'ı bu kullanıcı olan kayıt güncellenebilir (aksi halde 403). İstemciden
    /// bağlanmaz; controller rol/sahiplik kuralına göre doldurur (bkz. docs/ARCH.md §6.1).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class UpdateDriverHandler : ICommandHandler<UpdateDriverCommand>
{
    private readonly IRepository<Driver> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateDriverHandler(IRepository<Driver> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateDriverCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Driver", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.UserId != ownerId)
        {
            throw new ForbiddenException("Driver", request.Id);
        }

        entity.FullName = request.FullName;
        entity.Phone = request.Phone;
        entity.NationalId = request.NationalId;
        entity.LicenseNo = request.LicenseNo;
        entity.KvkkConsentAt = request.KvkkConsentAt;
        entity.LocationConsent = request.LocationConsent;
        entity.IsActive = request.IsActive;
        entity.CarrierId = request.CarrierId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Driver kaydını siler (soft delete).</summary>
public sealed class DeleteDriverCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>Doluysa yalnızca UserId'ı bu kullanıcı olan kayıt silinebilir (aksi halde 403). Controller doldurur.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class DeleteDriverHandler : ICommandHandler<DeleteDriverCommand>
{
    private readonly IRepository<Driver> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteDriverHandler(IRepository<Driver> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteDriverCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Driver", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.UserId != ownerId)
        {
            throw new ForbiddenException("Driver", request.Id);
        }
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
