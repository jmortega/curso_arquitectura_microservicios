namespace Strategy.Facturacion.Strategies;

using Strategy.Facturacion.Models;

/// <summary>
/// Estrategia configurable para países con un único tipo impositivo.
/// Permite registrar nuevos países sin crear una clase específica,
/// útil para tipos simples sin tramos ni exenciones especiales.
/// </summary>
public class EstrategiaImpuestoPersonalizada : IEstrategiaImpuesto
{
    private readonly string  _nombrePais;
    private readonly decimal _tipoGeneral;

    public string CodigoPais     { get; }
    public string NombreImpuesto { get; }

    public EstrategiaImpuestoPersonalizada(
        string  codigoPais,
        string  nombreImpuesto,
        string  nombrePais,
        decimal tipoGeneral)
    {
        CodigoPais     = codigoPais;
        NombreImpuesto = nombreImpuesto;
        _nombrePais    = nombrePais;
        _tipoGeneral   = tipoGeneral;
    }

    public ResultadoImpuesto Calcular(Factura factura)
    {
        decimal impuesto = factura.BaseImponible * _tipoGeneral;

        return new ResultadoImpuesto
        {
            BaseImponible    = factura.BaseImponible,
            BaseExenta       = factura.BaseExenta,
            ImporteImpuesto  = impuesto,
            TotalConImpuesto = factura.TotalBruto + impuesto,
            NombreImpuesto   = NombreImpuesto,
            PaisAplicado     = _nombrePais,
            Desglose         =
            [
                new DesgloseTipo($"Tipo general {_tipoGeneral:P0}",
                                 _tipoGeneral, impuesto)
            ]
        };
    }
}