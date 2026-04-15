using MassTransit;
using MassTransit.Pedidos;

namespace Consumidor.Consumers;

/// <summary>
/// Consumidor que procesa los mensajes de tipo <see cref="Pedido"/>
/// publicados por el Productor en RabbitMQ.
///
/// MassTransit crea automáticamente:
///   - Un exchange con el nombre del tipo: MassTransit.Pedidos:Pedido
///   - Una cola:  Consumidor.Consumers:PedidoConsumer
/// y enlaza ambos mediante un binding.
/// </summary>
public class PedidoConsumer : IConsumer<Pedido>
{
    private readonly ILogger<PedidoConsumer> _logger;

    public PedidoConsumer(ILogger<PedidoConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Pedido> context)
    {
        var pedido = context.Message;

        _logger.LogInformation(
            "[CONSUMIDOR] Mensaje recibido — PedidoId: {PedidoId} | Email: {Email} | Coste: {Coste:C2} | Fecha: {Fecha:u}",
            pedido.PedidoId,
            pedido.ClienteEmail,
            pedido.Coste,
            pedido.FechaCreacion);

        // ── Simular procesamiento ──────────────────────────────────────

        // 1. Validar datos mínimos del mensaje
        if (string.IsNullOrWhiteSpace(pedido.ClienteEmail))
        {
            _logger.LogWarning("[CONSUMIDOR] Pedido {PedidoId} descartado: email vacío.", pedido.PedidoId);
            return; // No lanzamos excepción → mensaje se marca como consumido
        }

        // 2. Simular envío de email de confirmación
        await EnviarEmailConfirmacionAsync(pedido);

        // 3. Simular actualización de inventario
        await ActualizarInventarioAsync(pedido);

        // 4. Simular registro en sistema de facturación
        await RegistrarEnFacturacionAsync(pedido);

        _logger.LogInformation(
            "[CONSUMIDOR] Pedido {PedidoId} procesado correctamente.",
            pedido.PedidoId);
    }

    // ------------------------------------------------------------------
    // Métodos privados que simulan integraciones externas
    // ------------------------------------------------------------------

    private async Task EnviarEmailConfirmacionAsync(Pedido pedido)
    {
        // En producción aquí iría: SmtpClient, SendGrid, etc.
        await Task.Delay(50); // simula latencia de red
        Console.WriteLine($"""
            ┌─ EMAIL ──────────────────────────────────────┐
              Para:    {pedido.ClienteEmail}
              Asunto:  Confirmación de pedido #{pedido.PedidoId}
              Cuerpo:  Su pedido por {pedido.Coste:C2} ha sido recibido.
            └──────────────────────────────────────────────┘
            """);
    }

    private async Task ActualizarInventarioAsync(Pedido pedido)
    {
        // En producción aquí se llamaría al servicio de inventario
        await Task.Delay(30);

        foreach (var linea in pedido.Lineas)
        {
            _logger.LogDebug(
                "[INVENTARIO] Descontando {Cantidad} unidad(es) de '{Producto}'.",
                linea.Cantidad,
                linea.Producto);
        }
    }

    private async Task RegistrarEnFacturacionAsync(Pedido pedido)
    {
        // En producción aquí se llamaría al ERP o sistema de facturación
        await Task.Delay(20);
        _logger.LogDebug(
            "[FACTURACIÓN] Pedido {PedidoId} registrado. Importe: {Coste:C2}",
            pedido.PedidoId,
            pedido.Coste);
    }
}