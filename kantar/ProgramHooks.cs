using BaseForge.Infrastructure.Messaging;

namespace Kantar;

/// <summary>
/// Program.cs genişletme noktaları (BaseForge.CodeGen üretir; bu dosya her üretimde ezilir). Gövdeleri elle,
/// ayrı bir dosyada yazın — örn. <c>Custom/ProgramHooks.Custom.cs</c>:
/// <code>
/// namespace Kantar;
/// internal static partial class ProgramHooks
/// {
///     static partial void ConfigureServices(WebApplicationBuilder builder) { /* builder.Services.Add... */ }
/// }
/// </code>
/// Yazılmayan kanca derlemede yok sayılır.
/// </summary>
internal static partial class ProgramHooks
{
    /// <summary>Servis kayıtları (Build öncesi).</summary>
    public static void Services(WebApplicationBuilder builder) => ConfigureServices(builder);

    /// <summary>Middleware ve endpoint'ler (MapControllers öncesi).</summary>
    public static void App(WebApplication app) => ConfigureApp(app);

    /// <summary>Ek RabbitMQ abonelikleri (CRUD dışı olaylar dahil): <c>mq.Subscribe&lt;T&gt;("servis/Olay", "kuyruk")</c>.</summary>
    public static void RabbitMq(RabbitMqOptions mq) => ConfigureRabbitMq(mq);

    static partial void ConfigureRabbitMq(RabbitMqOptions mq);

    static partial void ConfigureServices(WebApplicationBuilder builder);

    static partial void ConfigureApp(WebApplication app);
}
