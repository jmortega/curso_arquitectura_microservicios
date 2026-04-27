namespace Strategy.Facturacion.Context;

using Strategy.Facturacion.Models;
using Strategy.Facturacion.Strategies;

/// <summary>
/// Contexto que usa la estrategia de impuesto.
/// No conoce ni le importa qué estrategia concreta está usando:
/// solo llama a Calcular() a través de la interfaz.
/// La estrategia puede cambiarse en tiempo de ejecución.
/// </summary>
public class ContextoFacturacion
{
    private IEstrategiaImpuesto _estrategia;

    public string EstrategiaActual => _estrategia.NombreImpuesto;

    public ContextoFacturacion(IEstrategiaImpuesto estrategiaInicial)
        => _estrategia = estrategiaInicial;

    /// <summary>
    /// Cambia la estrategia en tiempo de ejecución sin
    /// necesidad de recrear el contexto.
    /// </summary>
    public void CambiarEstrategia(IEstrategiaImpuesto nuevaEstrategia)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n  ► Estrategia cambiada: " +
                          $"{_estrategia.NombreImpuesto} → {nuevaEstrategia.NombreImpuesto}");
        Console.ResetColor();
        _estrategia = nuevaEstrategia;
    }

    /// <summary>
    /// Delega el cálculo a la estrategia activa.
    /// El contexto no contiene lógica de impuestos.
    /// </summary>
    public ResultadoImpuesto CalcularImpuesto(Factura factura)
        => _estrategia.Calcular(factura);

    /// <summary>
    /// Genera la factura completa con el impuesto calculado.
    /// </summary>
    public ResumenFactura GenerarResumen(Factura factura)
    {
        var resultado = CalcularImpuesto(factura);

        return new ResumenFactura
        {
            FacturaId        = factura.Id,
            ClienteNombre    = factura.ClienteNombre,
            Fecha            = factura.Fecha,
            TotalBruto       = factura.TotalBruto,
            ResultadoFiscal  = resultado
        };
    }
}

public record ResumenFactura
{
    public Guid              FacturaId       { get; init; }
    public string            ClienteNombre   { get; init; } = string.Empty;
    public DateTime          Fecha           { get; init; }
    public decimal           TotalBruto      { get; init; }
    public ResultadoImpuesto ResultadoFiscal { get; init; } = null!;
}