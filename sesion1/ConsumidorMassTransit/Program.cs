using Consumidor.Configuration;
using Consumidor.Consumers;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

// ── Logging ────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ── MassTransit 7.x + RabbitMQ ────────────────────────────────────────
// En v7 AddMassTransit registra los tipos pero el bus se configura
// con AddMassTransitHostedService + Bus.Factory.CreateUsingRabbitMq
builder.Services.AddMassTransit(x =>
{
    // Registrar el consumidor en el contenedor de DI
    x.AddConsumer<PedidoConsumer>();
});

// Arrancar el bus como hosted service (equivalente a IHostedService)
builder.Services.AddMassTransitHostedService();

// Configurar el bus de RabbitMQ fuera de AddMassTransit (API v7)
builder.Services.AddSingleton<IBusControl>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<Program>>();
    var rabbitConfig = config.GetSection("RabbitMQ");

    var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
    {
        cfg.Host(
            host:        rabbitConfig["Host"]     ?? "localhost",
            virtualHost: rabbitConfig["VHost"]    ?? "/",
            h =>
            {
                h.Username(rabbitConfig["Username"] ?? "guest");
                h.Password(rabbitConfig["Password"] ?? "guest");
            });

        // Política de reintento global
        //cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));

        // Endpoint de la cola con configuración avanzada
        cfg.ConfigurarPedidosEndpoint(provider, config);
    });

    return bus;
});

// También registrar como IBus para inyección en otros servicios
builder.Services.AddSingleton<IBus>(provider =>
    provider.GetRequiredService<IBusControl>());

// ── Construir y ejecutar el Worker ────────────────────────────────────
var host = builder.Build();

// Arrancar el bus manualmente (necesario en v7 con este patrón)
var busControl = host.Services.GetRequiredService<IBusControl>();
await busControl.StartAsync();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Consumidor de Pedidos iniciado. Esperando mensajes en RabbitMQ...");

try
{
    await host.RunAsync();
}
finally
{
    await busControl.StopAsync();
}