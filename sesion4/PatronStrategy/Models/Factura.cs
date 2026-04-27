namespace Strategy.Facturacion.Models;

public record Factura
{
    public Guid                    Id            { get; init; } = Guid.NewGuid();
    public string                  ClienteNombre { get; init; } = string.Empty;
    public string                  ClienteEmail  { get; init; } = string.Empty;
    public string                  PaisIso       { get; init; } = string.Empty;
    public DateTime                Fecha         { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<LineaFactura> Lineas    { get; init; } = [];

    public decimal BaseImponible
        => Lineas.Where(l => !l.EstaExento).Sum(l => l.Subtotal);

    public decimal BaseExenta
        => Lineas.Where(l => l.EstaExento).Sum(l => l.Subtotal);

    public decimal TotalBruto
        => Lineas.Sum(l => l.Subtotal);
}