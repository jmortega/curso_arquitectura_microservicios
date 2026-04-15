namespace Productor.Endpoints;

/// <summary>
/// Payload que el cliente envía en el body del POST /pedidos.
/// </summary>
public record CrearPedidoRequest(
    string ClienteEmail,
    IReadOnlyList<LineaPedidoDto> Lineas
);

/// <summary>Línea de producto dentro de la solicitud.</summary>
public record LineaPedidoDto(
    string Producto,
    int Cantidad,
    decimal PrecioUnitario
);

/// <summary>Respuesta devuelta tras publicar el mensaje en el bus.</summary>
public record CrearPedidoResponse(
    string Message,
    Guid PedidoId,
    decimal CosteTotal,
    DateTime FechaCreacion
);