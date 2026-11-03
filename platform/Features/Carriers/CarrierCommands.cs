using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Platform.Entities;

namespace Platform.Features.Carriers;

/// <summary>Yeni bir Carrier oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateCarrierCommand : ICommand<Guid>
{
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    /// <summary>TaxNo.</summary>
    [MaxLength(11)]
    public string? TaxNo { get; set; }
    /// <summary>TaxOffice.</summary>
    [MaxLength(100)]
    public string? TaxOffice { get; set; }
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }
    /// <summary>Address.</summary>
    public string? Address { get; set; }
}

internal sealed class CreateCarrierHandler : ICommandHandler<CreateCarrierCommand, Guid>
{
    private readonly IRepository<Carrier> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateCarrierHandler(IRepository<Carrier> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCarrierCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Carrier
        {
            OwnerUserId = request.OwnerUserId,
            Name = request.Name,
            TaxNo = request.TaxNo,
            TaxOffice = request.TaxOffice,
            Phone = request.Phone,
            Address = request.Address,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Carrier kaydını günceller.</summary>
public sealed class UpdateCarrierCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>OwnerUserId.</summary>
    public Guid OwnerUserId { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    /// <summary>TaxNo.</summary>
    [MaxLength(11)]
    public string? TaxNo { get; set; }
    /// <summary>TaxOffice.</summary>
    [MaxLength(100)]
    public string? TaxOffice { get; set; }
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }
    /// <summary>Address.</summary>
    public string? Address { get; set; }

    /// <summary>
    /// Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıt güncellenebilir (aksi halde 403). İstemciden
    /// bağlanmaz; controller rol/sahiplik kuralına göre doldurur (bkz. docs/ARCH.md §6.1).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class UpdateCarrierHandler : ICommandHandler<UpdateCarrierCommand>
{
    private readonly IRepository<Carrier> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateCarrierHandler(IRepository<Carrier> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateCarrierCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Carrier", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.OwnerUserId != ownerId)
        {
            throw new ForbiddenException("Carrier", request.Id);
        }

        entity.Name = request.Name;
        entity.TaxNo = request.TaxNo;
        entity.TaxOffice = request.TaxOffice;
        entity.Phone = request.Phone;
        entity.Address = request.Address;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Carrier kaydını siler (soft delete).</summary>
public sealed class DeleteCarrierCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>Doluysa yalnızca OwnerUserId'ı bu kullanıcı olan kayıt silinebilir (aksi halde 403). Controller doldurur.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? RestrictToOwnerId { get; set; }
}

internal sealed class DeleteCarrierHandler : ICommandHandler<DeleteCarrierCommand>
{
    private readonly IRepository<Carrier> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteCarrierHandler(IRepository<Carrier> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCarrierCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Carrier", request.Id);
        if (request.RestrictToOwnerId is { } ownerId && entity.OwnerUserId != ownerId)
        {
            throw new ForbiddenException("Carrier", request.Id);
        }
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
