using BaseForge.Core.Exceptions;
using Kantar.Custom.Validation;
using Kantar.Features.Companys;
using Kantar.Features.Stations;
using MediatR.Pipeline;

namespace Kantar.Custom.Guards;

/// <summary>İstasyon e-İrsaliye sevk adresi olduğundan yeni ve düzenlenen kayıtta posta kodu zorunlu.</summary>
internal sealed class CreateStationAddressGuard : IRequestPreProcessor<CreateStationCommand>
{
    public Task Process(CreateStationCommand request, CancellationToken cancellationToken)
    {
        LocationRules.RequirePostalCode(request.PostalCode);
        return Task.CompletedTask;
    }
}

/// <inheritdoc cref="CreateStationAddressGuard" />
internal sealed class UpdateStationAddressGuard : IRequestPreProcessor<UpdateStationCommand>
{
    public Task Process(UpdateStationCommand request, CancellationToken cancellationToken)
    {
        LocationRules.RequirePostalCode(request.PostalCode);
        return Task.CompletedTask;
    }
}

/// <summary>Cari firma: VKN/TCKN ve posta kodu verilmişse geçerli olmalı.</summary>
internal static class CompanyIdentityRules
{
    public static void Validate(string? taxNo, string? postalCode)
    {
        if (!string.IsNullOrEmpty(taxNo) && !TaxIds.IsValidTaxNo(taxNo))
        {
            throw new ValidationException("TaxNo", "Geçerli bir vergi kimlik no (10 hane) ya da TC kimlik no (11 hane) girin.");
        }

        LocationRules.OptionalPostalCode(postalCode);
    }
}

internal sealed class CreateCompanyIdentityGuard : IRequestPreProcessor<CreateCompanyCommand>
{
    public Task Process(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        CompanyIdentityRules.Validate(request.TaxNo, request.PostalCode);
        return Task.CompletedTask;
    }
}

internal sealed class UpdateCompanyIdentityGuard : IRequestPreProcessor<UpdateCompanyCommand>
{
    public Task Process(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        CompanyIdentityRules.Validate(request.TaxNo, request.PostalCode);
        return Task.CompletedTask;
    }
}
