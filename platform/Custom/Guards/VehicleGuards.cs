using MediatR.Pipeline;
using Platform.Custom.Validation;
using Platform.Features.Vehicles;

namespace Platform.Custom.Guards;

/// <summary>Plaka her zaman anahtar biçiminde (34ABC123) saklanır; aramalar düz eşitlikle yapılır.</summary>
internal sealed class CreateVehiclePlateGuard : IRequestPreProcessor<CreateVehicleCommand>
{
    public Task Process(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        request.Plate = Plates.Key(request.Plate);
        return Task.CompletedTask;
    }
}

internal sealed class UpdateVehiclePlateGuard : IRequestPreProcessor<UpdateVehicleCommand>
{
    public Task Process(UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        request.Plate = Plates.Key(request.Plate);
        return Task.CompletedTask;
    }
}
