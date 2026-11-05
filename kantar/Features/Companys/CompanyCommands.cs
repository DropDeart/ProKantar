using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.Companys;

/// <summary>Yeni bir Company oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateCompanyCommand : ICommand<Guid>
{
    /// <summary>Name.</summary>
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    /// <summary>ShortName.</summary>
    [MaxLength(50)]
    public string? ShortName { get; set; }
    /// <summary>TaxNo.</summary>
    [MaxLength(11)]
    public string? TaxNo { get; set; }
    /// <summary>TaxOffice.</summary>
    [MaxLength(100)]
    public string? TaxOffice { get; set; }
    /// <summary>IsCustomer.</summary>
    public bool IsCustomer { get; set; } = false;
    /// <summary>IsSupplier.</summary>
    public bool IsSupplier { get; set; } = false;
    /// <summary>IsCarrier.</summary>
    public bool IsCarrier { get; set; } = false;
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }
    /// <summary>Email.</summary>
    [MaxLength(150)]
    public string? Email { get; set; }
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
    /// <summary>Bağlı ProKantar şirketi (firma rehberinden eklendiyse).</summary>
    public Guid? LinkedTenantId { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
}

internal sealed class CreateCompanyHandler : ICommandHandler<CreateCompanyCommand, Guid>
{
    private readonly IRepository<Company> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateCompanyHandler(IRepository<Company> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Company
        {
            Name = request.Name,
            ShortName = request.ShortName,
            TaxNo = request.TaxNo,
            TaxOffice = request.TaxOffice,
            IsCustomer = request.IsCustomer,
            IsSupplier = request.IsSupplier,
            IsCarrier = request.IsCarrier,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            City = request.City,
            District = request.District,
            PostalCode = request.PostalCode,
            LinkedTenantId = request.LinkedTenantId,
            IsActive = request.IsActive,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Company kaydını günceller.</summary>
public sealed class UpdateCompanyCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Name.</summary>
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    /// <summary>ShortName.</summary>
    [MaxLength(50)]
    public string? ShortName { get; set; }
    /// <summary>TaxNo.</summary>
    [MaxLength(11)]
    public string? TaxNo { get; set; }
    /// <summary>TaxOffice.</summary>
    [MaxLength(100)]
    public string? TaxOffice { get; set; }
    /// <summary>IsCustomer.</summary>
    public bool IsCustomer { get; set; } = false;
    /// <summary>IsSupplier.</summary>
    public bool IsSupplier { get; set; } = false;
    /// <summary>IsCarrier.</summary>
    public bool IsCarrier { get; set; } = false;
    /// <summary>Phone.</summary>
    [MaxLength(20)]
    public string? Phone { get; set; }
    /// <summary>Email.</summary>
    [MaxLength(150)]
    public string? Email { get; set; }
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
    /// <summary>Bağlı ProKantar şirketi (firma rehberinden eklendiyse).</summary>
    public Guid? LinkedTenantId { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
}

internal sealed class UpdateCompanyHandler : ICommandHandler<UpdateCompanyCommand>
{
    private readonly IRepository<Company> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateCompanyHandler(IRepository<Company> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Company", request.Id);
        entity.Name = request.Name;
        entity.ShortName = request.ShortName;
        entity.TaxNo = request.TaxNo;
        entity.TaxOffice = request.TaxOffice;
        entity.IsCustomer = request.IsCustomer;
        entity.IsSupplier = request.IsSupplier;
        entity.IsCarrier = request.IsCarrier;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.Address = request.Address;
        entity.City = request.City;
        entity.District = request.District;
        entity.PostalCode = request.PostalCode;
        entity.LinkedTenantId = request.LinkedTenantId;
        entity.IsActive = request.IsActive;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Company kaydını siler (soft delete).</summary>
public sealed class DeleteCompanyCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteCompanyHandler : ICommandHandler<DeleteCompanyCommand>
{
    private readonly IRepository<Company> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteCompanyHandler(IRepository<Company> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Company", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
