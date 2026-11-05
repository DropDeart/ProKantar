using BaseForge.Core.Exceptions;
using MediatR.Pipeline;
using Platform.Custom.Validation;
using Platform.Features.Drivers;

namespace Platform.Custom.Guards;

/// <summary>Şoför TCKN'si verilmişse sağlama algoritmasına uymalı (e-İrsaliye şoför bilgisi).</summary>
internal static class DriverRules
{
    public static void ValidateNationalId(string? nationalId)
    {
        if (!string.IsNullOrEmpty(nationalId) && !NationalIds.IsValidTckn(nationalId))
        {
            throw new ValidationException("NationalId", "Geçerli bir TC kimlik no (11 hane) girin.");
        }
    }
}

internal sealed class CreateDriverGuard : IRequestPreProcessor<CreateDriverCommand>
{
    public Task Process(CreateDriverCommand request, CancellationToken cancellationToken)
    {
        DriverRules.ValidateNationalId(request.NationalId);
        return Task.CompletedTask;
    }
}

internal sealed class UpdateDriverGuard : IRequestPreProcessor<UpdateDriverCommand>
{
    public Task Process(UpdateDriverCommand request, CancellationToken cancellationToken)
    {
        DriverRules.ValidateNationalId(request.NationalId);
        return Task.CompletedTask;
    }
}
