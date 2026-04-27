namespace Strategy.Facturacion.Strategies;

using Strategy.Facturacion.Models;

/// <summary>
/// Estrategia para países sin impuesto sobre ventas o zonas francas.
/// También sirve como estrategia nula (Null Object Pattern).
/// </summary>
public class EstrategiaImpuestoSinImpuesto : IEstrategiaImpuesto
{
    public string CodigoPais     => "XX";
    public string NombreImpuesto => "Sin impuesto";

    public ResultadoImpuesto Calcular(Factura factura) =>
        new()
        {
            BaseImponible    = factura.TotalBruto,
            BaseExenta       = 0m,
            ImporteImpuesto  = 0m,
            TotalConImpuesto = factura.TotalBruto,
            NombreImpuesto   = NombreImpuesto,
            PaisAplicado     = "Sin impuestos / Zona franca",
            Desglose         = []
        };
}