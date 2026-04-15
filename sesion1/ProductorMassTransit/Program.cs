using MassTransit;
using Productor.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// ── Swagger / OpenAPI ──────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "MassTransit Pedidos — Productor",
        Version     = "v1",
        Description = "API que publica mensajes de pedido en RabbitMQ mediante MassTransit 7"
    });
});

// ── MassTransit 7.x + RabbitMQ ────────────────────────────────────────
 // No se usa AddMassTransit porque el bus se configura y controla manualmente
 // con IBusControl en su propia registración más abajo.

// Configurar el bus con la fábrica de RabbitMQ (API v7)
builder.Services.AddSingleton<IBusControl>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var rabbitConfig = config.GetSection("RabbitMQ");

    return Bus.Factory.CreateUsingRabbitMq(cfg =>
    {
        cfg.Host(
            host:        rabbitConfig["Host"]     ?? "localhost",
            virtualHost: rabbitConfig["VHost"]    ?? "/",
            h =>
            {
                h.Username(rabbitConfig["Username"] ?? "guest");
                h.Password(rabbitConfig["Password"] ?? "guest");
            });

        // Política de reintento en publicación
        //cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromMilliseconds(500)));
    });
});

// Registrar IBus e IPublishEndpoint para inyectar en los endpoints
builder.Services.AddSingleton<IBus>(provider =>
    provider.GetRequiredService<IBusControl>());

builder.Services.AddSingleton<IPublishEndpoint>(provider =>
    provider.GetRequiredService<IBusControl>());

// ── Pipeline HTTP ──────────────────────────────────────────────────────
var app = builder.Build();

// Arrancar el bus al iniciar la aplicación
var busControl = app.Services.GetRequiredService<IBusControl>();
await busControl.StartAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Productor v1"));
}

// Registrar endpoints de pedidos
app.MapPedidosEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    Servicio = "Productor de Pedidos",
    Version  = "1.0 (MassTransit 7.x)",
    Swagger  = "/swagger"
})).ExcludeFromDescription();

app.Lifetime.ApplicationStopping.Register(() =>
    busControl.StopAsync().GetAwaiter().GetResult());

app.Run();