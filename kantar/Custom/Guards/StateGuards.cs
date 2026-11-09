using BaseForge.Core.Exceptions;
using Kantar.Custom.Validation;
using Kantar.Custom.Weighings;
using Kantar.Data;
using Kantar.Entities;
using Kantar.Features.Companys;
using Kantar.Features.DeliveryLocations;
using Kantar.Features.ShipmentStops;
using Kantar.Features.Shipments;
using Kantar.Features.WeighingAuditLogs;
using Kantar.Features.Weighings;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace Kantar.Custom.Guards;

/// <summary>
/// Generate edilen CRUD uçlarının durum kurallarını atlamasını engeller: tamamlanmış tartım ve yayınlanmış
/// sevkiyat bu uçlarla değiştirilemez/silinemez; durum geçişleri yalnız <c>Custom/</c> komutlarıyla yapılır.
/// </summary>
internal static class StateRules
{
    public static async Task RequireOpenWeighingAsync(KantarDbContext db, Guid id, string action, CancellationToken ct)
    {
        var status = await db.Weighings.Where(w => w.Id == id).Select(w => (WeighingStatus?)w.Status).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Weighing), id);
        if (status != WeighingStatus.Open)
        {
            throw new ValidationException(nameof(Weighing.Status), $"Kapanmış tartım {action}.");
        }
    }

    public static async Task RequireDraftShipmentAsync(KantarDbContext db, Guid id, string action, CancellationToken ct)
    {
        var status = await db.Shipments.Where(s => s.Id == id).Select(s => (ShipmentStatus?)s.Status).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Shipment), id);
        if (status != ShipmentStatus.Draft)
        {
            throw new ValidationException(nameof(Shipment.Status), $"Yayınlanmış sevkiyat {action}; gerekirse iptal edin.");
        }
    }

    /// <summary>Bağlanan sevkiyat aynı kiracıda olmalı (kiracı filtresi başka kiracının kaydını göstermez).</summary>
    public static async Task RequireOwnShipmentAsync(KantarDbContext db, Guid? id, CancellationToken ct)
    {
        if (id is { } shipmentId && !await db.Shipments.AnyAsync(s => s.Id == shipmentId, ct))
        {
            throw new NotFoundException(nameof(Shipment), shipmentId);
        }
    }
}

internal sealed class CreateWeighingGuard(KantarDbContext db) : IRequestPreProcessor<CreateWeighingCommand>
{
    public async Task Process(CreateWeighingCommand request, CancellationToken cancellationToken)
    {
        await StateRules.RequireOwnShipmentAsync(db, request.ShipmentId, cancellationToken);
        await WeighingShipmentLink.ApplyAsync(db, request, cancellationToken);
    }
}

internal sealed class UpdateWeighingGuard(KantarDbContext db) : IRequestPreProcessor<UpdateWeighingCommand>
{
    public async Task Process(UpdateWeighingCommand request, CancellationToken cancellationToken)
    {
        await StateRules.RequireOpenWeighingAsync(db, request.Id, "değiştirilemez", cancellationToken);
        await StateRules.RequireOwnShipmentAsync(db, request.ShipmentId, cancellationToken);

        // Açık tartımda da ilk tartımın ağırlığı, zamanı ve fiş no'su genel uçtan değişmez.
        var current = await db.Weighings.AsNoTracking()
            .Where(w => w.Id == request.Id)
            .Select(w => new { w.FirstWeightKg, w.FirstWeighedAt, w.FirstIsManual, w.TicketNo })
            .FirstAsync(cancellationToken);
        if (request.FirstWeightKg != current.FirstWeightKg || request.FirstWeighedAt != current.FirstWeighedAt
            || request.FirstIsManual != current.FirstIsManual || request.TicketNo != current.TicketNo)
        {
            throw new ValidationException(nameof(request.FirstWeightKg), "İlk tartımın ağırlığı, zamanı ve fiş numarası değiştirilemez.");
        }
    }
}

internal sealed class DeleteWeighingGuard(KantarDbContext db) : IRequestPreProcessor<DeleteWeighingCommand>
{
    public Task Process(DeleteWeighingCommand request, CancellationToken cancellationToken)
        => StateRules.RequireOpenWeighingAsync(db, request.Id, "silinemez", cancellationToken);
}

internal sealed class UpdateShipmentGuard(KantarDbContext db) : IRequestPreProcessor<UpdateShipmentCommand>
{
    public Task Process(UpdateShipmentCommand request, CancellationToken cancellationToken)
        => StateRules.RequireDraftShipmentAsync(db, request.Id, "değiştirilemez", cancellationToken);
}

internal sealed class DeleteShipmentGuard(KantarDbContext db) : IRequestPreProcessor<DeleteShipmentCommand>
{
    public Task Process(DeleteShipmentCommand request, CancellationToken cancellationToken)
        => StateRules.RequireDraftShipmentAsync(db, request.Id, "silinemez", cancellationToken);
}

internal sealed class CreateShipmentStopGuard(KantarDbContext db) : IRequestPreProcessor<CreateShipmentStopCommand>
{
    public Task Process(CreateShipmentStopCommand request, CancellationToken cancellationToken)
        => StateRules.RequireDraftShipmentAsync(db, request.ShipmentId, "durağı değiştirilemez", cancellationToken);
}

internal sealed class UpdateShipmentStopGuard(KantarDbContext db) : IRequestPreProcessor<UpdateShipmentStopCommand>
{
    public async Task Process(UpdateShipmentStopCommand request, CancellationToken cancellationToken)
    {
        // Durağın bugünkü ve (taşınıyorsa) yeni sevkiyatı taslak olmalı.
        var current = await db.ShipmentStops.Where(s => s.Id == request.Id).Select(s => (Guid?)s.ShipmentId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(ShipmentStop), request.Id);
        await StateRules.RequireDraftShipmentAsync(db, current, "durağı değiştirilemez", cancellationToken);
        if (request.ShipmentId != current)
        {
            await StateRules.RequireDraftShipmentAsync(db, request.ShipmentId, "durağı değiştirilemez", cancellationToken);
        }
    }
}

internal sealed class DeleteShipmentStopGuard(KantarDbContext db) : IRequestPreProcessor<DeleteShipmentStopCommand>
{
    public async Task Process(DeleteShipmentStopCommand request, CancellationToken cancellationToken)
    {
        var shipmentId = await db.ShipmentStops.Where(s => s.Id == request.Id).Select(s => (Guid?)s.ShipmentId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(ShipmentStop), request.Id);
        await StateRules.RequireDraftShipmentAsync(db, shipmentId, "durağı silinemez", cancellationToken);
    }
}

/// <summary>Denetim kaydı yalnız sunucu komutlarınca yazılır; istemci uydurma kayıt ekleyemez.</summary>
internal sealed class CreateWeighingAuditLogGuard : IRequestPreProcessor<CreateWeighingAuditLogCommand>
{
    public Task Process(CreateWeighingAuditLogCommand request, CancellationToken cancellationToken)
        => throw new ForbiddenException("Denetim kaydı yalnız sistem tarafından yazılır.");
}

/// <summary>Teslimat noktası: il, ilçe, posta kodu ve açık adres zorunlu; konum geçerli olmalı (varış tespiti için).</summary>
internal static class LocationRules
{
    public static void Validate(string? address, string? city, string? district, string? postalCode, decimal latitude, decimal longitude)
    {
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(district))
        {
            throw new ValidationException("City", "İl ve ilçe seçin.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ValidationException("Address", "Açık adresi girin.");
        }

        RequirePostalCode(postalCode);

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180 || (latitude == 0 && longitude == 0))
        {
            throw new ValidationException("Latitude", "Konumu haritada işaretleyin.");
        }
    }

    /// <summary>Posta kodu zorunlu ve GİB kuralına uygun olmalı (e-İrsaliye teslim adresi).</summary>
    public static void RequirePostalCode(string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new ValidationException("PostalCode", "Posta kodunu girin.");
        }

        OptionalPostalCode(postalCode);
    }

    /// <summary>Posta kodu verilmişse GİB kuralına uymalı.</summary>
    public static void OptionalPostalCode(string? postalCode)
    {
        if (!string.IsNullOrEmpty(postalCode) && !PostalCodes.IsValid(postalCode))
        {
            throw new ValidationException("PostalCode", PostalCodes.Error);
        }
    }
}

internal sealed class CreateDeliveryLocationGuard : IRequestPreProcessor<CreateDeliveryLocationCommand>
{
    public Task Process(CreateDeliveryLocationCommand request, CancellationToken cancellationToken)
    {
        LocationRules.Validate(request.Address, request.City, request.District, request.PostalCode, request.Latitude, request.Longitude);
        return Task.CompletedTask;
    }
}

internal sealed class UpdateDeliveryLocationGuard : IRequestPreProcessor<UpdateDeliveryLocationCommand>
{
    public Task Process(UpdateDeliveryLocationCommand request, CancellationToken cancellationToken)
    {
        LocationRules.Validate(request.Address, request.City, request.District, request.PostalCode, request.Latitude, request.Longitude);
        return Task.CompletedTask;
    }
}

/// <summary>Bir ProKantar şirketi cari listede en çok bir kez bağlanır.</summary>
internal static class CompanyLinkRules
{
    public static async Task RequireUniqueLinkAsync(KantarDbContext db, Guid? linkedTenantId, Guid? selfId, CancellationToken ct)
    {
        if (linkedTenantId is { } tenantId
            && await db.Companies.AnyAsync(c => c.LinkedTenantId == tenantId && c.Id != selfId, ct))
        {
            throw new ValidationException("LinkedTenantId", "Bu ProKantar firması cari listenizde zaten var.");
        }
    }
}

internal sealed class CreateCompanyLinkGuard(KantarDbContext db) : IRequestPreProcessor<CreateCompanyCommand>
{
    public Task Process(CreateCompanyCommand request, CancellationToken cancellationToken)
        => CompanyLinkRules.RequireUniqueLinkAsync(db, request.LinkedTenantId, null, cancellationToken);
}

internal sealed class UpdateCompanyLinkGuard(KantarDbContext db) : IRequestPreProcessor<UpdateCompanyCommand>
{
    public Task Process(UpdateCompanyCommand request, CancellationToken cancellationToken)
        => CompanyLinkRules.RequireUniqueLinkAsync(db, request.LinkedTenantId, request.Id, cancellationToken);
}
