namespace Strategy.Facturacion.Models;

public record ResultadoImpuesto
{
    public decimal BaseImponible    { get; init; }
    public decimal BaseExenta       { get; init; }
    public decimal ImporteImpuesto  { get; init; }
    public decimal TotalConImpuesto { get; init; }
    public string  NombreImpuesto   { get; init; } = string.Empty;
    public string  PaisAplicado     { get; init; } = string.Empty;

    // Desglose por tramos o tipos (útil para países con múltiples tipos)
    public IReadOnlyList<DesgloseTipo> Desglose { get; init; } = [];

    public void Mostrar()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"""

            ┌─── Resultado Fiscal: {PaisAplicado} ({NombreImpuesto}) ───────────┐
              Base imponible:    {BaseImponible,12:C2}
              Base exenta:       {BaseExenta,12:C2}
              Importe impuesto:  {ImporteImpuesto,12:C2}
              ────────────────────────────────────────
              TOTAL:             {TotalConImpuesto,12:C2}
            """);

        if (Desglose.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Desglose por tipos:");
            foreach (var d in Desglose)
                Console.WriteLine($"    {d.Descripcion,-30} {d.Tipo,6:P0}  {d.Importe,10:C2}");
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("└────────────────────────────────────────────────────┘");
        Console.ResetColor();
    }
}

public record DesgloseTipo(string Descripcion, decimal Tipo, decimal Importe);