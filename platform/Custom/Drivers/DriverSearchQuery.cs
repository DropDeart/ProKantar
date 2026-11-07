using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Platform.Custom.Validation;
using Platform.Data;

namespace Platform.Custom.Drivers;

/// <summary>Şoför arama ölçütü.</summary>
public enum DriverSearchBy
{
    /// <summary>Cep telefonu, birebir.</summary>
    Phone,

    /// <summary>Plaka, birebir; aracı kullanan tüm aktif şoförler döner.</summary>
    Plate,

    /// <summary>Ad soyad, içeren (en az 3 harf).</summary>
    Name,
}

/// <summary>
/// Operatörün sevkiyata şoför ataması için arama. Driver listesi yalnız sahibine açık olduğundan
/// sonuç az alanla ve sınırlı sayıda döner; telefon yalnız telefonla aranınca açık gösterilir.
/// </summary>
public sealed class DriverSearchQuery : IQuery<IReadOnlyList<DriverSearchItemDto>>
{
    /// <summary>Arama ölçütü.</summary>
    public DriverSearchBy By { get; set; }

    /// <summary>Aranan değer.</summary>
    public string Query { get; set; } = string.Empty;
}

/// <summary>Atama için gereken en az şoför bilgisi ve aktif kullandığı araçların plakaları.</summary>
public sealed record DriverSearchItemDto(Guid Id, string FullName, string Phone, bool LocationConsent, IReadOnlyList<string> Plates);

internal sealed class DriverSearchHandler(PlatformDbContext db) : IQueryHandler<DriverSearchQuery, IReadOnlyList<DriverSearchItemDto>>
{
    private const int MaxResults = 10;
    private const int MinNameLength = 3;

    public async Task<IReadOnlyList<DriverSearchItemDto>> Handle(DriverSearchQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var drivers = db.Drivers.AsNoTracking().Where(d => d.IsActive);

        switch (request.By)
        {
            case DriverSearchBy.Phone:
                var phone = Phones.Key(request.Query);
                if (phone.Length != 10)
                {
                    throw new ValidationException(nameof(request.Query), "Geçerli bir cep telefonu girin.");
                }

                drivers = drivers.Where(d => d.Phone == phone);
                break;

            case DriverSearchBy.Plate:
                var plate = Plates.Key(request.Query);
                if (plate.Length < 5)
                {
                    throw new ValidationException(nameof(request.Query), "Geçerli bir plaka girin.");
                }

                var driverIds = db.DriverVehicles
                    .Where(dv => dv.EndedAt == null && dv.Vehicle!.Plate == plate)
                    .Select(dv => dv.DriverId);
                drivers = drivers.Where(d => driverIds.Contains(d.Id));
                break;

            case DriverSearchBy.Name:
                var name = request.Query.Trim();
                if (name.Length < MinNameLength)
                {
                    throw new ValidationException(nameof(request.Query), $"Ad soyad için en az {MinNameLength} harf girin.");
                }

                drivers = drivers.Where(d => EF.Functions.ILike(d.FullName, $"%{EscapeLike(name)}%", "\\"));
                break;

            default:
                throw new ValidationException(nameof(request.By), "Arama ölçütü geçersiz.");
        }

        var showPhone = request.By == DriverSearchBy.Phone;
        return await DriverItems.ProjectAsync(db, drivers.OrderBy(d => d.FullName).Take(MaxResults), showPhone, cancellationToken);
    }

    private static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}

/// <summary>Şoför listelerinin ortak biçimi: az alan, maskeli telefon, aktif kullandığı araçların plakaları.</summary>
internal static class DriverItems
{
    /// <summary>Verilen sıradaki şoförleri DTO'ya çevirir.</summary>
    public static async Task<List<DriverSearchItemDto>> ProjectAsync(
        PlatformDbContext db, IQueryable<Platform.Entities.Driver> drivers, bool showPhone, CancellationToken cancellationToken)
    {
        var rows = await drivers
            .Select(d => new
            {
                d.Id,
                d.FullName,
                d.Phone,
                d.LocationConsent,
                Plates = db.DriverVehicles
                    .Where(dv => dv.DriverId == d.Id && dv.EndedAt == null)
                    .Select(dv => dv.Vehicle!.Plate)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new DriverSearchItemDto(r.Id, r.FullName, showPhone ? r.Phone : Phones.Mask(r.Phone), r.LocationConsent, r.Plates.Distinct().ToList()))
            .ToList();
    }
}
