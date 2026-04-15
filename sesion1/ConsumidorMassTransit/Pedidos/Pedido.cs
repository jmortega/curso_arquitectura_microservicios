namespace MassTransit.Pedidos;

/// <summary>
/// Contrato del mensaje. Debe ser idéntico al del Productor
/// y compartir el mismo namespace para que MassTransit
/// resuelva correctamente el tipo al deserializar.
///
/// En proyectos reales, este contrato se extrae a un
/// paquete NuGet compartido (p.ej. MassTransitPedidos.Contracts).
/// </summary>
public record Pedido
{
    public Guid PedidoId { get; init; }
    public string ClienteEmail { get; init; } = string.Empty;
    public decimal Coste { get; init; }
    public DateTime FechaCreacion { get; init; }
    public IReadOnlyList<LineaPedido> Lineas { get; init; } = [];
}

public record LineaPedido
{
    public string Producto { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
}