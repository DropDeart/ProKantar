using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kantar;

/// <summary>Elle yazılan servis kayıtları (generate dokunmaz).</summary>
internal static partial class ProgramHooks
{
    static partial void ConfigureServices(WebApplicationBuilder builder)
    {
        // MediatR ön işlemcileri kendiliğinden taramaz; generate edilen komutların kuralları (Custom/ altındaki
        // IRequestPreProcessor'lar: durum kuralları, kantar bağlantısı) burada bağlanır.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>)));
        var processors = typeof(ProgramHooks).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.Namespace?.StartsWith("Kantar.Custom", StringComparison.Ordinal) == true)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestPreProcessor<>))
                .Select(i => (Service: i, Implementation: t)));
        foreach (var (service, implementation) in processors)
        {
            builder.Services.AddTransient(service, implementation);
        }
    }
}
