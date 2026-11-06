using System.Text.Json.Serialization;
using BaseForge.Core.Serialization;
using BaseForge.Core.CQRS;
using BaseForge.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Platform.Custom.Drivers;
using Platform.Custom.Validation;
using Platform.Data;
using Platform.Entities;

namespace Platform.Custom.Fleet;

/// <summary>Azami ağırlığın kaynağı.</summary>
[JsonConverter(typeof(StrictStringEnumConverter<MaxGrossSource>))]
public enum MaxGrossSource
{
    /// <summary>Ruhsat değeri girilmiş.</summary>
    Registration,

    /// <summary>Katalog modelinin tipik değeri.</summary>
    Catalog,
}

/// <summary>Araç, katalog modeli ve geçerli azami yüklü ağırlık.</summary>
public sealed record VehicleSpecDto(
    Guid VehicleId,
    string Plate,
    bool IsClaimed,
    VehicleModelDto? Model,
    decimal? RegisteredMaxGrossKg,
    decimal? MaxGrossKg,
    MaxGrossSource? MaxGrossSource,
    bool InFleet,
    DriverSearchItemDto? DefaultDriver);

/// <summary>Aracın özelliği (model, ruhsat değeri, geçerli azami ağırlık).</summary>
public sealed class GetVehicleSpecQuery : IQuery<VehicleSpecDto>
{
    /// <summary>Araç.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Çağıranın firması (filo bilgisi için; controller doldurur).</summary>
    public Guid? TenantId { get; set; }
}

/// <summary>Şoförün kendi araçları: sahibi olduğu ya da aktif kullandığı araçlar (mobil).</summary>
public sealed class MyVehiclesQuery : IQuery<IReadOnlyList<VehicleSpecDto>>
{
    /// <summary>Çağıran (controller doldurur).</summary>
    public Guid UserId { get; set; }
}

/// <summary>Kim değiştirebilir: controller'ın doldurduğu çağıran bilgisi.</summary>
public sealed record VehicleEditor(Guid UserId, Guid? TenantId, bool IsStaff, bool IsSuperAdmin);

/// <summary>
/// Aracın katalog modelini ve ruhsat değerini yazar. Yetki: sahibi; firma personeli (araç kendi filosundaysa ya da
/// henüz bir şoför tarafından sahiplenilmemiş ve başka firmanın filosunda değilse); platform yöneticisi.
/// </summary>
public sealed class SetVehicleSpecCommand : ICommand
{
    /// <summary>Araç (rotadan).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid VehicleId { get; set; }

    /// <summary>Katalog modeli (boşsa bağ kaldırılır).</summary>
    public Guid? VehicleModelId { get; set; }

    /// <summary>Ruhsattaki azami yüklü ağırlık (kg; boşsa katalog değeri geçerli).</summary>
    public decimal? RegisteredMaxGrossKg { get; set; }

    /// <summary>Çağıran (controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public VehicleEditor? Editor { get; set; }
}

/// <summary>Araç özelliği okuma/yazma yardımcıları.</summary>
internal static class VehicleSpecs
{
    /// <summary>Araçları özellik ve modelleriyle DTO'ya çevirir.</summary>
    public static async Task<List<VehicleSpecDto>> ProjectAsync(
        PlatformDbContext db, IQueryable<Vehicle> vehicles, Guid? tenantId, CancellationToken cancellationToken)
    {
        var rows = await (
                from v in vehicles
                join s in db.VehicleSpecs on v.Id equals s.VehicleId into specs
                from s in specs.DefaultIfEmpty()
                orderby v.Plate
                select new { v.Id, v.Plate, v.IsClaimed, Spec = s, Model = s == null ? null : s.VehicleModel })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Varsayılan şoför yalnız aracın filosundaki firmaya gösterilir.
        var driverIds = rows
            .Where(r => tenantId is not null && r.Spec?.FleetTenantId == tenantId && r.Spec.DefaultDriverId is not null)
            .Select(r => r.Spec!.DefaultDriverId!.Value)
            .Distinct()
            .ToList();
        var drivers = driverIds.Count == 0
            ? new Dictionary<Guid, DriverSearchItemDto>()
            : (await DriverItems.ProjectAsync(db, db.Drivers.AsNoTracking().Where(d => driverIds.Contains(d.Id)), false, cancellationToken))
                .ToDictionary(d => d.Id);

        return rows.Select(r =>
        {
            var model = r.Model is { } m ? VehicleModelDto.From(m) : null;
            var registered = r.Spec?.RegisteredMaxGrossKg;
            var (maxGross, source) = registered is { } reg
                ? (reg, MaxGrossSource.Registration)
                : model is not null ? (model.MaxGrossKg, MaxGrossSource.Catalog) : ((decimal?)null, (MaxGrossSource?)null);
            var inFleet = tenantId is not null && r.Spec?.FleetTenantId == tenantId;
            var defaultDriver = inFleet && r.Spec!.DefaultDriverId is { } driverId ? drivers.GetValueOrDefault(driverId) : null;
            return new VehicleSpecDto(r.Id, r.Plate, r.IsClaimed, model, registered, maxGross, source, inFleet, defaultDriver);
        }).ToList();
    }

    /// <summary>Araca yeni (boş) özellik kaydı ekler.</summary>
    public static VehicleSpec AddFor(PlatformDbContext db, Guid vehicleId)
    {
        var spec = new VehicleSpec { VehicleId = vehicleId };
        db.VehicleSpecs.Add(spec);
        return spec;
    }

    /// <summary>Model seçilebilir mi (aktif ve onaylı ya da çağıranın önerisi) ve ruhsat değeri makul mü.</summary>
    public static async Task ValidateAsync(PlatformDbContext db, Guid? modelId, decimal? registeredMaxGrossKg, Guid userId, CancellationToken ct)
    {
        VehicleWeights.RequireGrossInRange(registeredMaxGrossKg, "RegisteredMaxGrossKg", "Ruhsattaki azami yüklü ağırlık");

        if (modelId is { } id && !await db.VehicleModels.AnyAsync(
                m => m.Id == id && m.IsActive && (m.IsApproved || m.SuggestedByUserId == userId), ct))
        {
            throw new ValidationException("VehicleModelId", "Seçilen araç modeli bulunamadı.");
        }
    }
}

internal sealed class GetVehicleSpecHandler(PlatformDbContext db) : IQueryHandler<GetVehicleSpecQuery, VehicleSpecDto>
{
    public async Task<VehicleSpecDto> Handle(GetVehicleSpecQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await VehicleSpecs.ProjectAsync(db, db.Vehicles.Where(v => v.Id == request.VehicleId), request.TenantId, cancellationToken);
        return result.FirstOrDefault() ?? throw new NotFoundException("Vehicle", request.VehicleId);
    }
}

internal sealed class MyVehiclesHandler(PlatformDbContext db) : IQueryHandler<MyVehiclesQuery, IReadOnlyList<VehicleSpecDto>>
{
    public async Task<IReadOnlyList<VehicleSpecDto>> Handle(MyVehiclesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var driving = db.DriverVehicles
            .Where(dv => dv.EndedAt == null && dv.Driver!.UserId == request.UserId)
            .Select(dv => dv.VehicleId);
        var vehicles = db.Vehicles.Where(v => v.OwnerUserId == request.UserId || driving.Contains(v.Id));
        return await VehicleSpecs.ProjectAsync(db, vehicles, null, cancellationToken);
    }
}

internal sealed class SetVehicleSpecHandler(PlatformDbContext db) : ICommandHandler<SetVehicleSpecCommand>
{
    public async Task Handle(SetVehicleSpecCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var editor = request.Editor ?? throw new ForbiddenException("Kullanıcı kimliği bulunamadı.");
        var vehicle = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken)
            ?? throw new NotFoundException("Vehicle", request.VehicleId);
        var spec = await db.VehicleSpecs.FirstOrDefaultAsync(s => s.VehicleId == vehicle.Id, cancellationToken);

        var isOwner = vehicle.OwnerUserId == editor.UserId;
        var fleetTenant = spec?.FleetTenantId;
        var staffMayEdit = editor.IsStaff && editor.TenantId is { } tenant
            && (fleetTenant == tenant || (fleetTenant is null && !vehicle.IsClaimed));
        if (!editor.IsSuperAdmin && !isOwner && !staffMayEdit)
        {
            throw new ForbiddenException("Vehicle", request.VehicleId);
        }

        await VehicleSpecs.ValidateAsync(db, request.VehicleModelId, request.RegisteredMaxGrossKg, editor.UserId, cancellationToken);
        spec ??= VehicleSpecs.AddFor(db, vehicle.Id);
        spec.VehicleModelId = request.VehicleModelId;
        spec.RegisteredMaxGrossKg = request.RegisteredMaxGrossKg;
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Firmanın kendi araçları ("Araçlarımız").</summary>
public sealed class FleetListQuery : IQuery<IReadOnlyList<VehicleSpecDto>>
{
    /// <summary>Firma (controller doldurur).</summary>
    public Guid TenantId { get; set; }
}

/// <summary>Firmanın filosuna araç ekler; plaka platformda yoksa açılır.</summary>
public sealed class AddFleetVehicleCommand : ICommand<Guid>
{
    /// <summary>Plaka.</summary>
    public string Plate { get; set; } = string.Empty;

    /// <summary>Katalog modeli.</summary>
    public Guid? VehicleModelId { get; set; }

    /// <summary>Ruhsattaki azami yüklü ağırlık (kg).</summary>
    public decimal? RegisteredMaxGrossKg { get; set; }

    /// <summary>Çağıran (controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid UserId { get; set; }

    /// <summary>Firma (controller doldurur).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid TenantId { get; set; }
}

/// <summary>Aracı firmanın filosundan çıkarır (araç ve özelliği platformda kalır).</summary>
public sealed class RemoveFleetVehicleCommand : ICommand
{
    /// <summary>Araç.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Firma (controller doldurur).</summary>
    public Guid TenantId { get; set; }
}

internal sealed class FleetListHandler(PlatformDbContext db) : IQueryHandler<FleetListQuery, IReadOnlyList<VehicleSpecDto>>
{
    public async Task<IReadOnlyList<VehicleSpecDto>> Handle(FleetListQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fleet = db.VehicleSpecs.Where(s => s.FleetTenantId == request.TenantId).Select(s => s.VehicleId);
        return await VehicleSpecs.ProjectAsync(db, db.Vehicles.Where(v => fleet.Contains(v.Id)), request.TenantId, cancellationToken);
    }
}

internal sealed class AddFleetVehicleHandler(PlatformDbContext db) : ICommandHandler<AddFleetVehicleCommand, Guid>
{
    public async Task<Guid> Handle(AddFleetVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plate = Plates.Key(request.Plate);
        if (!Plates.IsValid(plate))
        {
            throw new ValidationException(nameof(request.Plate), "Geçerli bir plaka girin (ör. 34 ABC 123).");
        }

        await VehicleSpecs.ValidateAsync(db, request.VehicleModelId, request.RegisteredMaxGrossKg, request.UserId, cancellationToken);

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Plate == plate, cancellationToken);
        VehicleSpec? spec = null;
        if (vehicle is null)
        {
            vehicle = new Vehicle { Plate = plate, OwnerUserId = request.UserId };
            db.Vehicles.Add(vehicle);
        }
        else
        {
            spec = await db.VehicleSpecs.FirstOrDefaultAsync(s => s.VehicleId == vehicle.Id, cancellationToken);
        }

        if (spec?.FleetTenantId is { } owner)
        {
            throw new ValidationException(nameof(request.Plate), owner == request.TenantId
                ? "Bu araç zaten araçlarınızda."
                : "Bu araç başka bir firmanın filosunda kayıtlı.");
        }

        spec ??= VehicleSpecs.AddFor(db, vehicle.Id);
        spec.FleetTenantId = request.TenantId;
        spec.VehicleModelId = request.VehicleModelId ?? spec.VehicleModelId;
        spec.RegisteredMaxGrossKg = request.RegisteredMaxGrossKg ?? spec.RegisteredMaxGrossKg;
        await db.SaveChangesAsync(cancellationToken);
        return vehicle.Id;
    }
}

internal sealed class RemoveFleetVehicleHandler(PlatformDbContext db) : ICommandHandler<RemoveFleetVehicleCommand>
{
    public async Task Handle(RemoveFleetVehicleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = await db.VehicleSpecs.FirstOrDefaultAsync(
                s => s.VehicleId == request.VehicleId && s.FleetTenantId == request.TenantId, cancellationToken)
            ?? throw new NotFoundException("Vehicle", request.VehicleId);
        spec.FleetTenantId = null;
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Filo aracına varsayılan şoför atar ya da kaldırır (şoför onayı gerekmez).</summary>
public sealed class SetFleetDriverCommand : ICommand
{
    /// <summary>Araç (rotadan).</summary>
    [JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid VehicleId { get; set; }

    /// <summary>Şoför; boşsa atama kaldırılır.</summary>
    public Guid? DriverId { get; set; }

    /// <summary>Firma (controller doldurur).</summary>
    [JsonIgnore]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Guid TenantId { get; set; }
}

internal sealed class SetFleetDriverHandler(PlatformDbContext db) : ICommandHandler<SetFleetDriverCommand>
{
    public async Task Handle(SetFleetDriverCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var spec = await db.VehicleSpecs.FirstOrDefaultAsync(
                s => s.VehicleId == request.VehicleId && s.FleetTenantId == request.TenantId, cancellationToken)
            ?? throw new NotFoundException("Vehicle", request.VehicleId);
        if (request.DriverId is { } driverId && !await db.Drivers.AnyAsync(d => d.Id == driverId && d.IsActive, cancellationToken))
        {
            throw new ValidationException(nameof(request.DriverId), "Şoför bulunamadı ya da pasif.");
        }

        spec.DefaultDriverId = request.DriverId;
        await db.SaveChangesAsync(cancellationToken);
    }
}
