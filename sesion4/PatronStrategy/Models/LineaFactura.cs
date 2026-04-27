namespace Strategy.Facturacion.Models;

public record LineaFactura
{
    public string    Descripcion    { get; init; } = string.Empty;
    public int       Cantidad       { get; init; }
    public decimal   PrecioUnitario { get; init; }
    public bool      EstaExento     { get; init; } // Exento de impuestos
    public string    CodigoProducto { get; init; } = string.Empty;

    public decimal Subtotal => Cantidad * PrecioUnitario;
}