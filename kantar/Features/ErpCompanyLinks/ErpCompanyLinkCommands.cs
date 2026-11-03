using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.ErpCompanyLinks;

/// <summary>Yeni bir ErpCompanyLink oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateErpCompanyLinkCommand : ICommand<Guid>
{
    /// <summary>ErpSystem.</summary>
    [MaxLength(30)]
    public string ErpSystem { get; set; } = string.Empty;
    /// <summary>ErpCode.</summary>
    [MaxLength(50)]
    public string ErpCode { get; set; } = string.Empty;
    /// <summary>IsDefault.</summary>
    public bool IsDefault { get; set; } = true;
    /// <summary>CompanyId.</summary>
    public Guid CompanyId { get; set; }
}

internal sealed class CreateErpCompanyLinkHandler : ICommandHandler<CreateErpCompanyLinkCommand, Guid>
{
    private readonly IRepository<ErpCompanyLink> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateErpCompanyLinkHandler(IRepository<ErpCompanyLink> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateErpCompanyLinkCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new ErpCompanyLink
        {
            ErpSystem = request.ErpSystem,
            ErpCode = request.ErpCode,
            IsDefault = request.IsDefault,
            CompanyId = request.CompanyId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir ErpCompanyLink kaydını günceller.</summary>
public sealed class UpdateErpCompanyLinkCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>ErpSystem.</summary>
    [MaxLength(30)]
    public string ErpSystem { get; set; } = string.Empty;
    /// <summary>ErpCode.</summary>
    [MaxLength(50)]
    public string ErpCode { get; set; } = string.Empty;
    /// <summary>IsDefault.</summary>
    public bool IsDefault { get; set; } = true;
    /// <summary>CompanyId.</summary>
    public Guid CompanyId { get; set; }
}

internal sealed class UpdateErpCompanyLinkHandler : ICommandHandler<UpdateErpCompanyLinkCommand>
{
    private readonly IRepository<ErpCompanyLink> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateErpCompanyLinkHandler(IRepository<ErpCompanyLink> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateErpCompanyLinkCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("ErpCompanyLink", request.Id);
        entity.ErpSystem = request.ErpSystem;
        entity.ErpCode = request.ErpCode;
        entity.IsDefault = request.IsDefault;
        entity.CompanyId = request.CompanyId;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir ErpCompanyLink kaydını siler (soft delete).</summary>
public sealed class DeleteErpCompanyLinkCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteErpCompanyLinkHandler : ICommandHandler<DeleteErpCompanyLinkCommand>
{
    private readonly IRepository<ErpCompanyLink> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteErpCompanyLinkHandler(IRepository<ErpCompanyLink> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteErpCompanyLinkCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("ErpCompanyLink", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
