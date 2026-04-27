namespace Strategy.Facturacion.Strategies;

using Strategy.Facturacion.Models;

/// <summary>
/// Sales Tax de EEUU. El tipo varía por estado.
/// Alimentación y medicamentos están exentos en la mayoría de estados.
/// No existe un impuesto federal sobre ventas unificado.
/// </summary>
public class EstrategiaImpuestoUSA : IEstrategiaImpuesto
{
    public string CodigoPais     => "US";
    public string NombreImpuesto => "Sales Tax (USA)";

    // Tipos por estado (muestra representativa)
    private static readonly Dictionary<string, decimal> TiposPorEstado = new()
    {
        ["CA"] = 0.0725m,  // California
        ["NY"] = 0.08m,    // Nueva York
        ["TX"] = 0.0625m,  // Texas
        ["FL"] = 0.06m,    // Florida
        ["WA"] = 0.065m,   // Washington
        ["OR"] = 0.00m,    // Oregon — sin Sales Tax
    };

    private const decimal TipoPorDefecto = 0.06m;

    private readonly string _estadoCliente;

    public EstrategiaImpuestoUSA(string estadoCliente = "CA")
        => _estadoCliente = estadoCliente.ToUpperInvariant();

    public ResultadoImpuesto Calcular(Factura factura)
    {
        decimal tipo = TiposPorEstado.GetValueOrDefault(_estadoCliente, TipoPorDefecto);

        // En EEUU solo tributan las líneas no exentas
        decimal baseImponible = factura.BaseImponible;
        decimal impuesto      = baseImponible * tipo;

        var desglose = new List<DesgloseTipo>
        {
            new($"Sales Tax — Estado {_estadoCliente}", tipo, impuesto)
        };

        return new ResultadoImpuesto
        {
            BaseImponible    = baseImponible,
            BaseExenta       = factura.BaseExenta,
            ImporteImpuesto  = impuesto,
            TotalConImpuesto = factura.TotalBruto + impuesto,
            NombreImpuesto   = NombreImpuesto,
            PaisAplicado     = $"Estados Unidos ({_estadoCliente})",
            Desglose         = desglose
        };
    }
}