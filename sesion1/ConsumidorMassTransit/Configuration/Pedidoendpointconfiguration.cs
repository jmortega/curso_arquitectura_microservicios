using Consumidor.Consumers;
using GreenPipes;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.RabbitMqTransport;

namespace Consumidor.Configuration;

/// <summary>
/// Extensión que centraliza la configuración del endpoint (cola)
/// del consumidor de pedidos, separándola del Program.cs.
///
/// NOTA: usa la API de MassTransit 7.x (gratuita, sin licencia).
/// En v7 el contexto de registro es IServiceProvider en lugar de
/// IBusRegistrationContext de v8.
/// </summary>
public static class PedidoEndpointConfiguration
{
    public static void ConfigurarPedidosEndpoint(
        this IRabbitMqBusFactoryConfigurator cfg,
        IServiceProvider provider,
        IConfiguration config)
    {
        var queueConfig = config.GetSection("RabbitMQ:Queue");
        string queueName       = queueConfig["Name"]             ?? "pedidos-notificaciones";
        int    prefetchCount   = int.Parse(queueConfig["PrefetchCount"]    ?? "10");
        int    concurrencyLimit = int.Parse(queueConfig["ConcurrencyLimit"] ?? "5");

        cfg.ReceiveEndpoint(queueName, e =>
        {
            // Cuántos mensajes se leen del broker antes de procesar
            e.PrefetchCount = (ushort)prefetchCount;

            // Política de reintento con backoff exponencial
            e.UseMessageRetry(r =>
            {
                r.Exponential(
                    retryLimit:    5,
                    minInterval:   TimeSpan.FromSeconds(1),
                    maxInterval:   TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(5));

                // No reintentar si el mensaje tiene datos inválidos
                r.Ignore<ArgumentNullException>();
                r.Ignore<InvalidOperationException>();
            });

            // Máximo de mensajes procesados en paralelo
            e.UseConcurrencyLimit(concurrencyLimit);

            // Cola de mensajes fallidos (dead-letter) — API v7
            e.BindDeadLetterQueue("pedidos-notificaciones-dead-letter");

            // Resolver el consumidor desde el contenedor DI (API v7)
            e.Consumer<PedidoConsumer>(provider);
        });
    }
}