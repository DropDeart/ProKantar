using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Platform.Entities;

namespace Platform.Features.Devices;

/// <summary>Yeni bir Device oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateDeviceCommand : ICommand<Guid>
{
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>DeviceIdentifier.</summary>
    [MaxLength(100)]
    public string DeviceIdentifier { get; set; } = string.Empty;
    /// <summary>Platform.</summary>
    public DevicePlatform Platform { get; set; }
    /// <summary>PushToken.</summary>
    [MaxLength(500)]
    public string? PushToken { get; set; }
    /// <summary>AppVersion.</summary>
    [MaxLength(20)]
    public string? AppVersion { get; set; }
    /// <summary>DriverId.</summary>
    public Guid DriverId { get; set; }
}

internal sealed class CreateDeviceHandler : ICommandHandler<CreateDeviceCommand, Guid>
{
    private readonly IRepository<Device> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateDeviceHandler(IRepository<Device> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Device
        {
            UserId = request.UserId,
            DeviceIdentifier = request.DeviceIdentifier,
            Platform = request.Platform,
            PushToken = request.PushToken,
            AppVersion = request.AppVersion,
            DriverId = request.DriverId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Device kaydını günceller.</summary>
public sealed class UpdateDeviceCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>UserId.</summary>
    public Guid UserId { get; set; }
    /// <summary>DeviceIdentifier.</summary>
    [MaxLength(100)]
    public string DeviceIdentifier { get; set; } = string.Empty;
    /// <summary>Platform.</summary>
    public DevicePlatform Platform { get; set; }
    /// <summary>PushToken.</summary>
    [MaxLength(500)]
    public string? PushToken { get; set; }
    /// <summary>AppVersion.</summary>
    [MaxLength(20)]
    public string? AppVersion { get; set; }
    /// <summary>DriverId.</summary>
    public Guid DriverId { get; set; }

    /// <summary>
    /// Doluysa yalnızca UserId'ı bu kullanıcı olan kayıt güncellenebilir (aksi halde 403). İstemciden
    /// bağlanmaz; controller rol/sahiplik kuralına göre doldurur (bkz. docs/ARCH.md §6.1).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class UpdateDeviceHandler : ICommandHandler<UpdateDeviceCommand>
{
    private readonly IRepository<Device> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateDeviceHandler(IRepository<Device> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Device", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.UserId != ownerId)
        {
            throw new ForbiddenException("Device", request.Id);
        }

        entity.DeviceIdentifier = request.DeviceIdentifier;
        entity.Platform = request.Platform;
        entity.PushToken = request.PushToken;
        entity.AppVersion = request.AppVersion;
        entity.DriverId = request.DriverId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Device kaydını siler (soft delete).</summary>
public sealed class DeleteDeviceCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>Doluysa yalnızca UserId'ı bu kullanıcı olan kayıt silinebilir (aksi halde 403). Controller doldurur.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class DeleteDeviceHandler : ICommandHandler<DeleteDeviceCommand>
{
    private readonly IRepository<Device> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteDeviceHandler(IRepository<Device> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Device", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.UserId != ownerId)
        {
            throw new ForbiddenException("Device", request.Id);
        }
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
