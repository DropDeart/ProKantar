using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.Materials;

/// <summary>Yeni bir Material oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateMaterialCommand : ICommand<Guid>
{
    /// <summary>Code.</summary>
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    /// <summary>Name.</summary>
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Unit.</summary>
    public MaterialUnit Unit { get; set; }
    /// <summary>KgPerUnit.</summary>
    public decimal? KgPerUnit { get; set; }
    /// <summary>ErpStockCode.</summary>
    [MaxLength(50)]
    public string? ErpStockCode { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
}

internal sealed class CreateMaterialHandler : ICommandHandler<CreateMaterialCommand, Guid>
{
    private readonly IRepository<Material> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateMaterialHandler(IRepository<Material> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new Material
        {
            Code = request.Code,
            Name = request.Name,
            Unit = request.Unit,
            KgPerUnit = request.KgPerUnit,
            ErpStockCode = request.ErpStockCode,
            IsActive = request.IsActive,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

/// <summary>Var olan bir Material kaydını günceller.</summary>
public sealed class UpdateMaterialCommand : ICommand
{
    /// <summary>Güncellenecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
    /// <summary>Code.</summary>
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    /// <summary>Name.</summary>
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    /// <summary>Unit.</summary>
    public MaterialUnit Unit { get; set; }
    /// <summary>KgPerUnit.</summary>
    public decimal? KgPerUnit { get; set; }
    /// <summary>ErpStockCode.</summary>
    [MaxLength(50)]
    public string? ErpStockCode { get; set; }
    /// <summary>IsActive.</summary>
    public bool IsActive { get; set; } = true;
}

internal sealed class UpdateMaterialHandler : ICommandHandler<UpdateMaterialCommand>
{
    private readonly IRepository<Material> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public UpdateMaterialHandler(IRepository<Material> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateMaterialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Material", request.Id);
        entity.Code = request.Code;
        entity.Name = request.Name;
        entity.Unit = request.Unit;
        entity.KgPerUnit = request.KgPerUnit;
        entity.ErpStockCode = request.ErpStockCode;
        entity.IsActive = request.IsActive;
        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Bir Material kaydını siler (soft delete).</summary>
public sealed class DeleteMaterialCommand : ICommand
{
    /// <summary>Silinecek kaydın kimliği.</summary>
    public Guid Id { get; set; }
}

internal sealed class DeleteMaterialHandler : ICommandHandler<DeleteMaterialCommand>
{
    private readonly IRepository<Material> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteMaterialHandler(IRepository<Material> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteMaterialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Material", request.Id);
        await _repository.DeleteAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
