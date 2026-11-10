using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Platform.Data;
using Platform.Entities;

namespace Platform.Custom.Fleet;

/// <summary>Katalog modeli.</summary>
public sealed record VehicleModelDto(
    Guid Id, string Brand, string Name, VehicleBodyType BodyType, string? AxleConfig, int AxleCount,
    decimal MaxGrossKg, decimal? TypicalTareKg, decimal? AvgFuelLPer100Km, bool IsApproved, bool IsActive)
{
    /// <summary>Entity'den DTO.</summary>
    public static VehicleModelDto From(VehicleModel m) => new(
        m.Id, m.Brand, m.Name, m.BodyType, m.AxleConfig, m.AxleCount, m.MaxGrossKg, m.TypicalTareKg, m.AvgFuelLPer100Km, m.IsApproved, m.IsActive);
}

/// <summary>Katalogdaki markalar (onaylı ve seçilebilir modeli olanlar), alfabetik.</summary>
public sealed class VehicleModelBrandsQuery : IQuery<IReadOnlyList<string>>;

/// <summary>
/// Seçilebilir modeller: onaylı ve aktif olanlar ile çağıranın kendi önerileri. Yönetici <see cref="IncludePending"/>
/// ile onay bekleyenleri de görür.
/// </summary>
public sealed class VehicleModelListQuery : IQuery<IReadOnlyList<VehicleModelDto>>
{
    /// <summary>Marka (birebir).</summary>
    public string? Brand { get; set; }

    /// <summary>Yapı.</summary>
    public VehicleBodyType? BodyType { get; set; }

    /// <summary>Marka/model içinde arama.</summary>
    public string? Search { get; set; }

    /// <summary>Yalnız onay bekleyen (etkin) öneriler; yalnız yönetici (<see cref="IncludePending"/> ile birlikte).</summary>
    public bool OnlyPending { get; set; }

    /// <summary>Onay bekleyenler de dahil (yalnız yönetici; controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public bool IncludePending { get; set; }

    /// <summary>Çağıran (kendi önerileri için; controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid? UserId { get; set; }
}

/// <summary>Katalog modeli alanları (öneri ve yönetici düzeltmesi ortak).</summary>
public abstract class VehicleModelFields
{
    /// <summary>Marka.</summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>Model / versiyon.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Yapı.</summary>
    public VehicleBodyType BodyType { get; set; }

    /// <summary>Çekiş düzeni (ör. "6x4").</summary>
    public string? AxleConfig { get; set; }

    /// <summary>Dingil sayısı.</summary>
    public int AxleCount { get; set; }

    /// <summary>Azami yüklü ağırlık (kg).</summary>
    public decimal MaxGrossKg { get; set; }

    /// <summary>Tipik boş ağırlık (kg).</summary>
    public decimal? TypicalTareKg { get; set; }

    /// <summary>Ortalama yakıt (L/100 km).</summary>
    public decimal? AvgFuelLPer100Km { get; set; }
}

/// <summary>Katalogda olmayan model önerisi; yönetici onaylayana kadar yalnız önerene görünür.</summary>
public sealed class SuggestVehicleModelCommand : VehicleModelFields, ICommand<Guid>
{
    /// <summary>Öneren (controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid UserId { get; set; }
}

/// <summary>Yönetici: katalog modelini düzeltir, onaylar ya da pasife alır.</summary>
public sealed class UpdateVehicleModelCommand : VehicleModelFields, ICommand
{
    /// <summary>Model (rotadan).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid Id { get; set; }

    /// <summary>Onaylı.</summary>
    public bool IsApproved { get; set; }

    /// <summary>Seçilebilir.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Katalog alan kuralları.</summary>
internal static class VehicleModelRules
{
    public static void Validate(VehicleModelFields f)
    {
        f.Brand = f.Brand.Trim();
        f.Name = f.Name.Trim();
        f.AxleConfig = string.IsNullOrWhiteSpace(f.AxleConfig) ? null : f.AxleConfig.Trim().ToLowerInvariant();
        if (f.Brand.Length is 0 or > 50)
        {
            throw new ValidationException(nameof(f.Brand), "Marka girin (en çok 50 karakter).");
        }

        if (f.Name.Length is 0 or > 80)
        {
            throw new ValidationException(nameof(f.Name), "Model girin (en çok 80 karakter).");
        }

        if (!Enum.IsDefined(f.BodyType))
        {
            throw new ValidationException(nameof(f.BodyType), "Araç yapısı geçersiz.");
        }

        if (f.AxleConfig is { Length: > 10 })
        {
            throw new ValidationException(nameof(f.AxleConfig), "Çekiş düzeni en çok 10 karakter (ör. 6x4).");
        }

        if (f.AxleCount is < 2 or > 10)
        {
            throw new ValidationException(nameof(f.AxleCount), "Dingil sayısı 2 ile 10 arasında olmalı.");
        }

        VehicleWeights.RequireGrossInRange(f.MaxGrossKg, nameof(f.MaxGrossKg), "Azami yüklü ağırlık");

        if (f.TypicalTareKg is { } tare && (tare <= 0 || tare >= f.MaxGrossKg))
        {
            throw new ValidationException(nameof(f.TypicalTareKg), "Boş ağırlık sıfırdan büyük, azami ağırlıktan küçük olmalı.");
        }

        if (f.AvgFuelLPer100Km is <= 0 or > 100)
        {
            throw new ValidationException(nameof(f.AvgFuelLPer100Km), "Ortalama yakıt 0 ile 100 L/100 km arasında olmalı.");
        }
    }

    public static void Apply(VehicleModelFields f, VehicleModel m)
    {
        m.Brand = f.Brand;
        m.Name = f.Name;
        m.BodyType = f.BodyType;
        m.AxleConfig = f.AxleConfig;
        m.AxleCount = f.AxleCount;
        m.MaxGrossKg = f.MaxGrossKg;
        m.TypicalTareKg = f.TypicalTareKg;
        m.AvgFuelLPer100Km = f.AvgFuelLPer100Km;
    }
}

internal sealed class VehicleModelBrandsHandler(PlatformDbContext db) : IQueryHandler<VehicleModelBrandsQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(VehicleModelBrandsQuery request, CancellationToken cancellationToken)
        => await db.VehicleModels.AsNoTracking()
            .Where(m => m.IsApproved && m.IsActive)
            .Select(m => m.Brand)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync(cancellationToken);
}

internal sealed class VehicleModelListHandler(PlatformDbContext db) : IQueryHandler<VehicleModelListQuery, IReadOnlyList<VehicleModelDto>>
{
    private const int MaxResults = 200;

    public async Task<IReadOnlyList<VehicleModelDto>> Handle(VehicleModelListQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var models = db.VehicleModels.AsNoTracking();
        if (!request.IncludePending)
        {
            var userId = request.UserId;
            models = models.Where(m => (m.IsApproved && m.IsActive) || (userId != null && m.SuggestedByUserId == userId));
        }
        else if (request.OnlyPending)
        {
            // Reddedilen öneri pasife alınır; listede yalnız karar bekleyenler kalır.
            models = models.Where(m => !m.IsApproved && m.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            models = models.Where(m => m.Brand == request.Brand);
        }

        if (request.BodyType is { } bodyType)
        {
            models = models.Where(m => m.BodyType == bodyType);
        }

        if (LikePattern.ContainsOrNull(request.Search) is { } pattern)
        {
            models = models.Where(m => EF.Functions.ILike(m.Brand + " " + m.Name, pattern, LikePattern.EscapeCharacter));
        }

        return await models
            .OrderBy(m => m.Brand).ThenBy(m => m.Name)
            .Take(MaxResults)
            .Select(m => VehicleModelDto.From(m))
            .ToListAsync(cancellationToken);
    }
}

internal sealed class SuggestVehicleModelHandler(PlatformDbContext db) : ICommandHandler<SuggestVehicleModelCommand, Guid>
{
    public async Task<Guid> Handle(SuggestVehicleModelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        VehicleModelRules.Validate(request);
        var duplicate = await db.VehicleModels.AsNoTracking()
            .Where(m => m.BodyType == request.BodyType
                && m.Brand.ToLower() == request.Brand.ToLower()
                && m.Name.ToLower() == request.Name.ToLower()
                && (m.IsApproved || m.SuggestedByUserId == request.UserId))
            .Select(m => (Guid?)m.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (duplicate is { } id)
        {
            return id;
        }

        var model = new VehicleModel { IsApproved = false, SuggestedByUserId = request.UserId };
        VehicleModelRules.Apply(request, model);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync(cancellationToken);
        return model.Id;
    }
}

internal sealed class UpdateVehicleModelHandler(PlatformDbContext db) : ICommandHandler<UpdateVehicleModelCommand>
{
    public async Task Handle(UpdateVehicleModelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        VehicleModelRules.Validate(request);
        var model = await db.VehicleModels.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("VehicleModel", request.Id);
        VehicleModelRules.Apply(request, model);
        model.IsApproved = request.IsApproved;
        model.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
    }
}
