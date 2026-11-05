using BaseForge.Core.Exceptions;
using Kantar.Features.Waybills;
using MediatR.Pipeline;

namespace Kantar.Custom.Guards;

/// <summary>
/// İrsaliye yalnız <c>POST api/Weighings/{id}/despatch</c> ile oluşur; generate edilen create/update/delete uçları
/// kapalıdır (kesilmiş belge değiştirilemez, silinemez).
/// </summary>
internal static class WaybillRules
{
    public const string Message = "İrsaliye yalnız tartımdan kesilir; kesilmiş irsaliye değiştirilemez ve silinemez.";
}

internal sealed class CreateWaybillGuard : IRequestPreProcessor<CreateWaybillCommand>
{
    public Task Process(CreateWaybillCommand request, CancellationToken cancellationToken) => throw new ForbiddenException(WaybillRules.Message);
}

internal sealed class UpdateWaybillGuard : IRequestPreProcessor<UpdateWaybillCommand>
{
    public Task Process(UpdateWaybillCommand request, CancellationToken cancellationToken) => throw new ForbiddenException(WaybillRules.Message);
}

internal sealed class DeleteWaybillGuard : IRequestPreProcessor<DeleteWaybillCommand>
{
    public Task Process(DeleteWaybillCommand request, CancellationToken cancellationToken) => throw new ForbiddenException(WaybillRules.Message);
}
