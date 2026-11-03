using System.Globalization;
using Grpc.Core;
using MediatR;
using Kantar.Features.Machines;

namespace Kantar.Grpc;

/// <summary>
/// Machine entity'sine diğer servislerin salt-okunur gRPC erişimi
/// (BaseForge.CodeGen tarafından üretildi; mevcut CQRS sorgusu üzerinden veri okur).
/// </summary>
public sealed class MachineGrpcService(ISender sender) : MachineService.MachineServiceBase
{
    public override async Task<MachineMessage> GetById(MachineByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Geçersiz id."));
        }

        var value = await sender.Send(new GetMachineByIdQuery { Id = id }, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Machine bulunamadı: {id}"));

        return new MachineMessage
        {
            Id = value.Id.ToString(),
            Name = value.Name,
            MachineType = value.MachineType,
            PlateOrSerial = value.PlateOrSerial ?? string.Empty,
            IsActive = value.IsActive,
        };
    }
}
