using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Platform.Custom.Validation;
using Platform.Data;
using Platform.Entities;

namespace Platform.Custom.Drivers;

/// <summary>"Şoförlerimiz" satırı. Telefon şoför kabul edene kadar maskelidir.</summary>
public sealed record TenantDriverDto(
    Guid Id,
    TenantDriverStatus Status,
    TenantDriverSource Source,
    DateTimeOffset InvitedAt,
    DateTimeOffset? RespondedAt,
    Guid? DriverId,
    string? FullName,
    string Phone,
    bool? LocationConsent,
    IReadOnlyList<string> Plates,
    string? InviteToken);

/// <summary>Davet linkinin açtığı sayfa için özet (oturumsuz): firma, maskeli numara, durum.</summary>
public sealed record PublicInvitationDto(string TenantName, string Phone, TenantDriverStatus Status, DateTimeOffset InvitedAt);

/// <summary>Şoföre gösterilen davet.</summary>
public sealed record DriverInvitationDto(Guid Id, string TenantName, DateTimeOffset InvitedAt);

/// <summary>Yakındaki şoför için firmaya gösterilen özet: ad, araç tipi, firmayla bağın durumu (telefon yok).</summary>
public sealed record DriverSummaryDto(Guid UserId, Guid DriverId, string FullName, IReadOnlyList<DriverVehicleBrief> Vehicles, TenantDriverStatus? Status);

/// <summary>Şoförün kullandığı aracın katalog özeti.</summary>
public sealed record DriverVehicleBrief(VehicleBodyType BodyType, string Brand, string Name);

/// <summary>Firmanın şoförleri ve bekleyen davetleri (çıkarılanlar hariç).</summary>
public sealed class TenantDriversQuery : IQuery<IReadOnlyList<TenantDriverDto>>
{
    /// <summary>Firma (controller doldurur).</summary>
    public Guid TenantId { get; set; }
}

/// <summary>Şoför davet eder: kayıtlı şoför (<see cref="DriverId"/>) ya da telefon (<see cref="Phone"/>) ile.</summary>
public sealed class InviteDriverCommand : ICommand<Guid>
{
    /// <summary>Kayıtlı şoför.</summary>
    public Guid? DriverId { get; set; }

    /// <summary>Cep telefonu (şoför kayıtlı değilse SMS ile uygulama linki gider).</summary>
    public string? Phone { get; set; }

    /// <summary>Kaynak (arama, telefon, yakındakiler).</summary>
    public TenantDriverSource Source { get; set; }

    /// <summary>Firma (controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid TenantId { get; set; }

    /// <summary>Firma adı (controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public string TenantName { get; set; } = string.Empty;

    /// <summary>Davet eden (controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid UserId { get; set; }
}

/// <summary>Daveti geri çeker ya da şoförü firmadan çıkarır.</summary>
public sealed class RemoveTenantDriverCommand : ICommand
{
    /// <summary>Kayıt.</summary>
    public Guid Id { get; set; }

    /// <summary>Firma (controller doldurur).</summary>
    public Guid TenantId { get; set; }
}

/// <summary>Yakındaki müsait şoförlerin (tracking'den gelen kullanıcı kimlikleri) firmaya gösterilecek özeti.</summary>
public sealed class DriverSummariesQuery : IQuery<IReadOnlyList<DriverSummaryDto>>
{
    /// <summary>Kullanıcı kimlikleri (en çok 50).</summary>
    public List<Guid> UserIds { get; set; } = [];

    /// <summary>Firma (controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid TenantId { get; set; }
}

/// <summary>Şoförün bekleyen davetleri (mobil). Telefon token'daki doğrulanmış numaradan gelir.</summary>
public sealed class MyInvitationsQuery : IQuery<IReadOnlyList<DriverInvitationDto>>
{
    /// <summary>Çağıran.</summary>
    public Guid UserId { get; set; }

    /// <summary>Doğrulanmış telefon (token).</summary>
    public string? VerifiedPhone { get; set; }
}

/// <summary>Şoför daveti kabul eder ya da reddeder (mobil).</summary>
public sealed class RespondInvitationCommand : ICommand
{
    /// <summary>Davet.</summary>
    public Guid Id { get; set; }

    /// <summary>Kabul mü.</summary>
    public bool Accept { get; set; }

    /// <summary>Çağıran.</summary>
    public Guid UserId { get; set; }

    /// <summary>Doğrulanmış telefon (token).</summary>
    public string? VerifiedPhone { get; set; }
}

/// <summary>Davet linkinden davetin özeti (oturum gerekmez).</summary>
public sealed class InvitationByTokenQuery : IQuery<PublicInvitationDto>
{
    /// <summary>Link kodu.</summary>
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Davet linkinden kabul/ret. Davet çağıranın şoför kaydına ya da doğrulanmış telefonuna gelmiş olmalı. Şoför profili
/// yoksa kabulde <see cref="FullName"/> ve doğrulanmış numarayla açılır.
/// </summary>
public sealed class RespondInvitationByTokenCommand : ICommand
{
    /// <summary>Link kodu.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public string Token { get; set; } = string.Empty;

    /// <summary>Kabul mü.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public bool Accept { get; set; }

    /// <summary>Şoför profili yoksa açılacak profilin adı.</summary>
    public string? FullName { get; set; }

    /// <summary>Çağıran (controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid UserId { get; set; }

    /// <summary>Doğrulanmış telefon (token; controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public string? VerifiedPhone { get; set; }
}

/// <summary>Davet sorgu yardımcıları.</summary>
internal static class TenantDrivers
{
    /// <summary>Firmada bekleyen ya da aktif kaydı olanlar (aynı numaraya ikinci davet açılmaz).</summary>
    public static IQueryable<TenantDriver> Open(this IQueryable<TenantDriver> rows)
        => rows.Where(t => t.Status == TenantDriverStatus.Invited || t.Status == TenantDriverStatus.Active);

    /// <summary>Çağıran şoföre ait bekleyen davetler: şoför kaydına ya da doğrulanmış telefonuna gelenler.</summary>
    public static IQueryable<TenantDriver> PendingFor(this IQueryable<TenantDriver> rows, Guid? driverId, string? verifiedPhone)
    {
        var phone = verifiedPhone is null ? null : Phones.Key(verifiedPhone);
        return rows.Where(t => t.Status == TenantDriverStatus.Invited && ((driverId != null && t.DriverId == driverId) || (phone != null && t.Phone == phone)));
    }
}

internal sealed class TenantDriversHandler(PlatformDbContext db) : IQueryHandler<TenantDriversQuery, IReadOnlyList<TenantDriverDto>>
{
    public async Task<IReadOnlyList<TenantDriverDto>> Handle(TenantDriversQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rows = await db.TenantDrivers.AsNoTracking()
            .Where(t => t.TenantId == request.TenantId && t.Status != TenantDriverStatus.Removed)
            .OrderBy(t => t.Status).ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
        var driverIds = rows.Where(r => r.DriverId is not null).Select(r => r.DriverId!.Value).Distinct().ToList();
        var drivers = (await DriverItems.ProjectAsync(db, db.Drivers.AsNoTracking().Where(d => driverIds.Contains(d.Id)), true, cancellationToken))
            .ToDictionary(d => d.Id);

        return rows.Select(r =>
        {
            var driver = r.DriverId is { } id ? drivers.GetValueOrDefault(id) : null;
            var phone = r.Status == TenantDriverStatus.Active ? r.Phone : Phones.Mask(r.Phone);
            return new TenantDriverDto(r.Id, r.Status, r.Source, r.CreatedAt, r.RespondedAt, r.DriverId, driver?.FullName, phone,
                driver?.LocationConsent, driver?.Plates ?? [], r.Status == TenantDriverStatus.Invited ? r.InviteToken : null);
        }).ToList();
    }
}

internal sealed class InviteDriverHandler(PlatformDbContext db, IDriverNotifier notifier) : ICommandHandler<InviteDriverCommand, Guid>
{
    public async Task<Guid> Handle(InviteDriverCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Driver? driver;
        string phone;
        if (request.DriverId is { } driverId)
        {
            driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driverId && d.IsActive, cancellationToken)
                ?? throw new ValidationException(nameof(request.DriverId), "Şoför bulunamadı ya da pasif.");
            phone = Phones.Key(driver.Phone);
        }
        else
        {
            phone = Phones.Key(request.Phone);
            if (!Phones.IsValidMobile(phone))
            {
                throw new ValidationException(nameof(request.Phone), "Geçerli bir cep telefonu girin.");
            }

            driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Phone == phone && d.IsActive, cancellationToken);
        }

        var existing = await db.TenantDrivers.Open()
            .Where(t => t.TenantId == request.TenantId && (t.Phone == phone || (driver != null && t.DriverId == driver.Id)))
            .Select(t => (TenantDriverStatus?)t.Status)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is { } status)
        {
            throw new ValidationException(nameof(request.Phone), status == TenantDriverStatus.Active
                ? "Bu şoför zaten şoförlerinizde."
                : "Bu şoföre bekleyen bir davetiniz var.");
        }

        var invitation = new TenantDriver
        {
            TenantId = request.TenantId,
            TenantName = request.TenantName,
            DriverId = driver?.Id,
            Phone = phone,
            Status = TenantDriverStatus.Invited,
            Source = request.Source,
            InvitedByUserId = request.UserId,
            InviteToken = InviteTokens.New(),
        };
        db.TenantDrivers.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);
        await notifier.InvitedAsync(phone, request.TenantName, invitation.InviteToken, cancellationToken);
        return invitation.Id;
    }
}

/// <summary>Davet linki kodu: 16 rastgele bayt, URL-güvenli Base64 (22 karakter).</summary>
internal static class InviteTokens
{
    public static string New()
        => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

internal sealed class InvitationByTokenHandler(PlatformDbContext db) : IQueryHandler<InvitationByTokenQuery, PublicInvitationDto>
{
    public async Task<PublicInvitationDto> Handle(InvitationByTokenQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Geri çekilen davet linki hiçbir şey göstermez.
        var row = await db.TenantDrivers.AsNoTracking()
                .FirstOrDefaultAsync(t => t.InviteToken == request.Token && t.Status != TenantDriverStatus.Removed, cancellationToken)
            ?? throw new NotFoundException("Davet", request.Token);
        return new PublicInvitationDto(row.TenantName, Phones.Mask(row.Phone), row.Status, row.CreatedAt);
    }
}

internal sealed class RespondInvitationByTokenHandler(PlatformDbContext db) : ICommandHandler<RespondInvitationByTokenCommand>
{
    public async Task Handle(RespondInvitationByTokenCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var invitation = await db.TenantDrivers
                .FirstOrDefaultAsync(t => t.InviteToken == request.Token && t.Status != TenantDriverStatus.Removed, cancellationToken)
            ?? throw new NotFoundException("Davet", request.Token);
        if (invitation.Status != TenantDriverStatus.Invited)
        {
            throw new ValidationException("Invitation", invitation.Status == TenantDriverStatus.Active ? "Bu davet zaten kabul edildi." : "Bu davet yanıtlandı.");
        }

        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.UserId == request.UserId, cancellationToken);
        var phone = request.VerifiedPhone is null ? null : Phones.Key(request.VerifiedPhone);
        var mine = (driver is not null && invitation.DriverId == driver.Id) || (phone is not null && phone == invitation.Phone);
        if (!mine)
        {
            throw new ValidationException("Phone",
                $"Bu davet {Phones.Mask(invitation.Phone)} numarasına gönderildi. Daveti kabul etmek için bu numarayı hesabınızda doğrulayın.");
        }

        if (request.Accept && driver is null)
        {
            var fullName = request.FullName?.Trim();
            if (string.IsNullOrEmpty(fullName) || fullName.Length < 3)
            {
                throw new ValidationException(nameof(request.FullName), "Şoför profiliniz için adınızı ve soyadınızı girin.");
            }

            // Aynı numarayla başka kullanıcının şoför profili varsa davet ona aittir; ikinci profil açılmaz.
            if (await db.Drivers.AnyAsync(d => d.Phone == invitation.Phone && d.IsActive, cancellationToken))
            {
                throw new ValidationException("Phone", "Bu numarayla kayıtlı başka bir şoför profili var.");
            }

            driver = new Driver { UserId = request.UserId, FullName = fullName, Phone = invitation.Phone, IsActive = true };
            db.Drivers.Add(driver);
        }

        invitation.DriverId = driver?.Id ?? invitation.DriverId;
        invitation.Status = request.Accept ? TenantDriverStatus.Active : TenantDriverStatus.Declined;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class RemoveTenantDriverHandler(PlatformDbContext db) : ICommandHandler<RemoveTenantDriverCommand>
{
    public async Task Handle(RemoveTenantDriverCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var row = await db.TenantDrivers.FirstOrDefaultAsync(t => t.Id == request.Id && t.TenantId == request.TenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(TenantDriver), request.Id);
        row.Status = TenantDriverStatus.Removed;
        await db.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class DriverSummariesHandler(PlatformDbContext db) : IQueryHandler<DriverSummariesQuery, IReadOnlyList<DriverSummaryDto>>
{
    private const int MaxIds = 50;

    public async Task<IReadOnlyList<DriverSummaryDto>> Handle(DriverSummariesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userIds = request.UserIds.Distinct().Take(MaxIds).ToList();
        var rows = await db.Drivers.AsNoTracking()
            .Where(d => d.IsActive && userIds.Contains(d.UserId))
            .Select(d => new
            {
                d.Id,
                d.UserId,
                d.FullName,
                Vehicles = db.DriverVehicles
                    .Where(dv => dv.DriverId == d.Id && dv.EndedAt == null)
                    .Select(dv => db.VehicleSpecs
                        .Where(s => s.VehicleId == dv.VehicleId && s.VehicleModel != null)
                        .Select(s => new DriverVehicleBrief(s.VehicleModel!.BodyType, s.VehicleModel.Brand, s.VehicleModel.Name))
                        .FirstOrDefault())
                    .ToList(),
                Status = db.TenantDrivers.Open()
                    .Where(t => t.TenantId == request.TenantId && t.DriverId == d.Id)
                    .Select(t => (TenantDriverStatus?)t.Status)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new DriverSummaryDto(
                r.UserId,
                r.Id,
                r.FullName,
                r.Vehicles.OfType<DriverVehicleBrief>().Distinct().ToList(),
                r.Status))
            .ToList();
    }
}

internal sealed class MyInvitationsHandler(PlatformDbContext db) : IQueryHandler<MyInvitationsQuery, IReadOnlyList<DriverInvitationDto>>
{
    public async Task<IReadOnlyList<DriverInvitationDto>> Handle(MyInvitationsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var driverId = await db.Drivers.Where(d => d.UserId == request.UserId).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(cancellationToken);
        return await db.TenantDrivers.AsNoTracking()
            .PendingFor(driverId, request.VerifiedPhone)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new DriverInvitationDto(t.Id, t.TenantName, t.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

internal sealed class RespondInvitationHandler(PlatformDbContext db) : ICommandHandler<RespondInvitationCommand>
{
    public async Task Handle(RespondInvitationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == request.UserId, cancellationToken);
        var invitation = await db.TenantDrivers
                .PendingFor(driver?.Id, request.VerifiedPhone)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TenantDriver), request.Id);
        if (request.Accept && driver is null)
        {
            throw new ValidationException("Driver", "Daveti kabul etmek için önce şoför profilinizi oluşturun.");
        }

        invitation.DriverId = driver?.Id ?? invitation.DriverId;
        invitation.Status = request.Accept ? TenantDriverStatus.Active : TenantDriverStatus.Declined;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
