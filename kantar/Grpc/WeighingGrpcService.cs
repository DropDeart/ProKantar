using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.Weighings;

namespace Kantar.Grpc;

/// <summary>
/// Weighing entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class WeighingGrpcService(ISender sender) : WeighingService.WeighingServiceBase
{
    public override async Task<WeighingMessage> GetById(WeighingByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetWeighingByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Weighing bulunamadı: {id}"));

        return new WeighingMessage
        {
            Id = value.Id.ToString(),
            TicketNo = value.TicketNo,
            Direction = value.Direction.ToString(),
            Status = value.Status.ToString(),
            Plate = value.Plate,
            TrailerId = value.TrailerId?.ToString() ?? string.Empty,
            TrailerPlate = value.TrailerPlate ?? string.Empty,
            DriverId = value.DriverId?.ToString() ?? string.Empty,
            DriverName = value.DriverName ?? string.Empty,
            DriverPhone = value.DriverPhone ?? string.Empty,
            FirstWeightKg = value.FirstWeightKg.ToString(CultureInfo.InvariantCulture),
            FirstWeighedAt = value.FirstWeighedAt.ToString("o", CultureInfo.InvariantCulture),
            FirstIsManual = value.FirstIsManual,
            SecondWeighbridgeId = value.SecondWeighbridgeId?.ToString() ?? string.Empty,
            SecondWeightKg = value.SecondWeightKg?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            SecondWeighedAt = value.SecondWeighedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
            SecondIsManual = value.SecondIsManual,
            GrossKg = value.GrossKg?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            TareKg = value.TareKg?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            NetKg = value.NetKg?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            TareSource = value.TareSource?.ToString() ?? string.Empty,
            CancelReason = value.CancelReason ?? string.Empty,
            Note = value.Note ?? string.Empty,
        };
    }
}
