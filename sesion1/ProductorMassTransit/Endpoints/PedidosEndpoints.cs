using MassTransit;
using MassTransit.Pedidos;

namespace Productor.Endpoints;

/// <summary>
/// Extensión que registra todos los endpoints relacionados con pedidos.
/// Mantiene Program.cs limpio y facilita la organización por feature.
/// </summary>
public static class PedidosEndpoints
{
    public static void MapPedidosEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/pedidos")
                       .WithTags("Pedidos")
                       .WithOpenApi();

        // ── POST /pedidos ──────────────────────────────────────────────
        group.MapPost("/", CrearPedidoAsync)
             .WithName("CrearPedido")
             .WithSummary("Publica un nuevo pedido en el bus de mensajes")
             .WithDescription("Genera un Guid para el pedido, calcula el coste total y publica el mensaje en RabbitMQ. El Consumidor lo procesará de forma asíncrona.");

        // ── POST /pedidos/demo ─────────────────────────────────────────
        group.MapPost("/demo", CrearPedidoDemoAsync)
             .WithName("CrearPedidoDemo")
             .WithSummary("Publica un pedido de ejemplo sin necesidad de body");
    }

    // ------------------------------------------------------------------
    // Handler: pedido con datos reales desde el body
    // ------------------------------------------------------------------
    private static async Task<IResult> CrearPedidoAsync(
        CrearPedidoRequest request,
        IPublishEndpoint publishEndpoint)
    {
        // Validación básica
        if (string.IsNullOrWhiteSpace(request.ClienteEmail))
            return Results.BadRequest("El email del cliente es obligatorio.");

        if (request.Lineas is null || request.Lineas.Count == 0)
            return Results.BadRequest("El pedido debe tener al menos una línea.");

        // Mapeo del DTO al contrato del mensaje
        var lineas = request.Lineas.Select(l => new LineaPedido
        {
            Producto       = l.Producto,
            Cantidad       = l.Cantidad,
            PrecioUnitario = l.PrecioUnitario
        }).ToList();

        decimal costeTotal = lineas.Sum(l => l.Cantidad * l.PrecioUnitario);

        var evento = new Pedido
        {
            PedidoId      = Guid.NewGuid(),
            ClienteEmail  = request.ClienteEmail,
            Coste         = costeTotal,
            FechaCreacion = DateTime.UtcNow,
            Lineas        = lineas
        };

        // Publicar en el bus — MassTransit enruta al exchange correcto
        await publishEndpoint.Publish(evento);

        Console.WriteLine($"[PRODUCTOR] Pedido {evento.PedidoId} publicado en el bus para {evento.ClienteEmail}");

        return Results.Ok(new CrearPedidoResponse(
            Message:       "Pedido enviado al bus correctamente",
            PedidoId:      evento.PedidoId,
            CosteTotal:    costeTotal,
            FechaCreacion: evento.FechaCreacion
        ));
    }

    // ------------------------------------------------------------------
    // Handler: pedido de demo sin body (para pruebas rápidas desde Swagger)
    // ------------------------------------------------------------------
    private static async Task<IResult> CrearPedidoDemoAsync(
        IPublishEndpoint publishEndpoint)
    {
        var evento = new Pedido
        {
            PedidoId     = Guid.NewGuid(),
            ClienteEmail = "demo@ejemplo.com",
            Coste        = 249.95m,
            FechaCreacion = DateTime.UtcNow,
            Lineas       =
            [
                new LineaPedido { Producto = "Teclado Mecánico", Cantidad = 1, PrecioUnitario = 89.95m },
                new LineaPedido { Producto = "Ratón Inalámbrico", Cantidad = 2, PrecioUnitario = 80.00m }
            ]
        };

        await publishEndpoint.Publish(evento);

        Console.WriteLine($"[PRODUCTOR] Pedido DEMO {evento.PedidoId} publicado en el bus");

        return Results.Ok(new CrearPedidoResponse(
            Message:       "Pedido de demo enviado al bus",
            PedidoId:      evento.PedidoId,
            CosteTotal:    evento.Coste,
            FechaCreacion: evento.FechaCreacion
        ));
    }
}