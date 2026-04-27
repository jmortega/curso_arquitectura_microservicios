namespace Strategy.Facturacion.Strategies;

using Strategy.Facturacion.Models;

/// <summary>
/// Contrato que deben implementar todas las estrategias de cálculo
/// de impuestos. Cada implementación encapsula las reglas fiscales
/// de un país o región sin que el cliente necesite conocerlas.
/// </summary>
public interface IEstrategiaImpuesto
{
    /// <summary>Código ISO del país al que aplica esta estrategia.</summary>
    string CodigoPais { get; }

    /// <summary>Nombre del impuesto en el país (IVA, VAT, Sales Tax...).</summary>
    string NombreImpuesto { get; }

    /// <summary>
    /// Calcula el impuesto aplicable a la factura proporcionada.
    /// </summary>
    ResultadoImpuesto Calcular(Factura factura);
}