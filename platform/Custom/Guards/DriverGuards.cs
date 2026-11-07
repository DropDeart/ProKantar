using BaseForge.Core.Exceptions;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
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

/// <summary>Kullanıcı başına tek şoför profili (davet, arama ve atamalar şoförü kullanıcıdan bulur).</summary>
internal sealed class CreateDriverGuard(PlatformDbContext db) : IRequestPreProcessor<CreateDriverCommand>
{
    public async Task Process(CreateDriverCommand request, CancellationToken cancellationToken)
    {
        request.Phone = Phones.Key(request.Phone);
        DriverRules.ValidateNationalId(request.NationalId);
        if (await db.Drivers.AnyAsync(d => d.UserId == request.UserId, cancellationToken))
        {
            throw new ValidationException("UserId", "Bu hesabın şoför profili zaten var; güncelleyin.");
        }
    }
}

internal sealed class UpdateDriverGuard : IRequestPreProcessor<UpdateDriverCommand>
{
    public Task Process(UpdateDriverCommand request, CancellationToken cancellationToken)
    {
        request.Phone = Phones.Key(request.Phone);
        DriverRules.ValidateNationalId(request.NationalId);
        return Task.CompletedTask;
    }
}
