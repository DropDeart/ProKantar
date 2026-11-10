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

/// <summary>
/// Şoför profilinin telefonu yalnız çağıranın doğrulanmış numarasıdır (token'daki <c>phone_number</c>; identity yalnız SMS ile
/// doğrulanınca yazar). Aksi halde başkasının numarasıyla açılan profil, o numaraya giden davetleri üstlenebilirdi.
/// </summary>
internal static class VerifiedDriverPhone
{
    /// <summary>Doğrulanmış numaranın anahtarı; yoksa ya da istekteki numarayla uyuşmuyorsa 400.</summary>
    public static string Require(IHttpContextAccessor http, string? requested)
    {
        var verified = http.HttpContext?.User.FindFirst("phone_number")?.Value is { Length: > 0 } claim ? Phones.Key(claim) : null;
        if (verified is null)
        {
            throw new ValidationException("Phone", "Şoför profili için önce telefon numaranızı SMS koduyla doğrulayın.");
        }

        var phone = Phones.Key(requested);
        if (phone.Length > 0 && phone != verified)
        {
            throw new ValidationException("Phone", "Şoför profilinin telefonu doğruladığınız numara olmalı.");
        }

        return verified;
    }
}

/// <summary>Kullanıcı başına tek şoför profili (davet, arama ve atamalar şoförü kullanıcıdan bulur); telefon doğrulanmış olmalı.</summary>
internal sealed class CreateDriverGuard(PlatformDbContext db, IHttpContextAccessor http) : IRequestPreProcessor<CreateDriverCommand>
{
    public async Task Process(CreateDriverCommand request, CancellationToken cancellationToken)
    {
        request.Phone = VerifiedDriverPhone.Require(http, request.Phone);
        DriverRules.ValidateNationalId(request.NationalId);
        if (await db.Drivers.AnyAsync(d => d.UserId == request.UserId, cancellationToken))
        {
            throw new ValidationException("UserId", "Bu hesabın şoför profili zaten var; güncelleyin.");
        }
    }
}

/// <summary>Profil telefonu değişecekse yeni numara çağıranın doğrulanmış numarası olmalı.</summary>
internal sealed class UpdateDriverGuard(PlatformDbContext db, IHttpContextAccessor http) : IRequestPreProcessor<UpdateDriverCommand>
{
    public async Task Process(UpdateDriverCommand request, CancellationToken cancellationToken)
    {
        request.Phone = Phones.Key(request.Phone);
        DriverRules.ValidateNationalId(request.NationalId);
        var current = await db.Drivers.AsNoTracking().Where(d => d.Id == request.Id).Select(d => d.Phone).FirstOrDefaultAsync(cancellationToken);
        if (current is not null && Phones.Key(current) != request.Phone)
        {
            request.Phone = VerifiedDriverPhone.Require(http, request.Phone);
        }
    }
}
