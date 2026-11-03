using System.ComponentModel.DataAnnotations;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using Kantar.Entities;

namespace Kantar.Features.WeighingAuditLogs;

/// <summary>Yeni bir WeighingAuditLog oluşturur; üretilen kimliği döndürür.</summary>
public sealed class CreateWeighingAuditLogCommand : ICommand<Guid>
{
    /// <summary>Action.</summary>
    public WeighingAuditLogAction Action { get; set; }
    /// <summary>FieldName.</summary>
    [MaxLength(50)]
    public string? FieldName { get; set; }
    /// <summary>OldValue.</summary>
    [MaxLength(250)]
    public string? OldValue { get; set; }
    /// <summary>NewValue.</summary>
    [MaxLength(250)]
    public string? NewValue { get; set; }
    /// <summary>Reason.</summary>
    [MaxLength(250)]
    public string? Reason { get; set; }
    /// <summary>WeighingId.</summary>
    public Guid WeighingId { get; set; }
}

internal sealed class CreateWeighingAuditLogHandler : ICommandHandler<CreateWeighingAuditLogCommand, Guid>
{
    private readonly IRepository<WeighingAuditLog> _repository;
    private readonly IUnitOfWork _unitOfWork;
    public CreateWeighingAuditLogHandler(IRepository<WeighingAuditLog> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateWeighingAuditLogCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = new WeighingAuditLog
        {
            Action = request.Action,
            FieldName = request.FieldName,
            OldValue = request.OldValue,
            NewValue = request.NewValue,
            Reason = request.Reason,
            WeighingId = request.WeighingId,
        };
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}


