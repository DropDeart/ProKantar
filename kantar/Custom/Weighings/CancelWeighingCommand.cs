using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using BaseForge.Core.Interfaces;
using BaseForge.Core.Messaging;
using Kantar.Data;
using Kantar.Entities;
using Kantar.Features.Weighings;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Weighings;

/// <summary>Açık tartımı gerekçeyle iptal eder; denetim kaydı yazılır.</summary>
public sealed class CancelWeighingCommand : ICommand
{
    /// <summary>Tartım kimliği (route'tan).</summary>
    public Guid Id { get; set; }

    /// <summary>İptal gerekçesi (zorunlu).</summary>
    public string? Reason { get; set; }
}

internal sealed class CancelWeighingHandler(KantarDbContext db, IUnitOfWork unitOfWork, IEventBus eventBus)
    : ICommandHandler<CancelWeighingCommand>
{
    private const int MaxReasonLength = 250;

    public async Task Handle(CancelWeighingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var reason = request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > MaxReasonLength)
        {
            throw new ValidationException(nameof(request.Reason), $"İptal gerekçesi zorunlu (en çok {MaxReasonLength} karakter).");
        }

        // Tamamlama ile yarışmasın: satır kilitlenir.
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Weighings\" WHERE \"Id\" = {request.Id} FOR UPDATE", cancellationToken);

        var weighing = await db.Weighings.FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Weighing), request.Id);
        if (weighing.Status != WeighingStatus.Open)
        {
            throw new ValidationException(nameof(Weighing.Status), "Yalnızca açık tartım iptal edilebilir.");
        }

        weighing.Status = WeighingStatus.Cancelled;
        weighing.CancelReason = reason;
        db.WeighingAuditLogs.Add(new WeighingAuditLog
        {
            WeighingId = weighing.Id,
            Action = WeighingAuditLogAction.Cancel,
            FieldName = nameof(Weighing.Status),
            OldValue = nameof(WeighingStatus.Open),
            NewValue = nameof(WeighingStatus.Cancelled),
            Reason = reason,
        });

        await eventBus.PublishAsync(new WeighingUpdatedEvent { Data = WeighingDto.From(weighing) }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
