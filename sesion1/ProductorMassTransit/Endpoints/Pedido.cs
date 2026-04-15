namespace MassTransit.Pedidos;

/// <summary>
/// Contrato del mensaje que viaja por el bus.
/// Se usa como record para garantizar inmutabilidad.
/// Tanto el Productor como el Consumidor deben tener
/// esta clase con el mismo namespace para que MassTransit
/// pueda resolver el tipo del mensaje.
/// </summary>
public record Pedido
{
    /// <summary>Identificador único del pedido generado en el Productor.</summary>
    public Guid PedidoId { get; init; }

    /// <summary>Email del cliente al que se notificará.</summary>
    public string ClienteEmail { get; init; } = string.Empty;

    /// <summary>Coste total del pedido en euros.</summary>
    public decimal Coste { get; init; }

    /// <summary>Fecha y hora de creación del pedido (UTC).</summary>
    public DateTime FechaCreacion { get; init; }

    /// <summary>Líneas de detalle del pedido.</summary>
    public IReadOnlyList<LineaPedido> Lineas { get; init; } = [];
}

/// <summary>Detalle de cada producto dentro del pedido.</summary>
public record LineaPedido
{
    public string Producto { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
}