namespace Strategy.Facturacion.Strategies;

using Strategy.Facturacion.Models;

/// <summary>
/// VAT del Reino Unido post-Brexit: estándar 20%, reducido 5%
/// (energía doméstica, higiene infantil), exento 0% (alimentos,
/// libros, ropa infantil, medicamentos).
/// </summary>
public class EstrategiaImpuestoReinoUnido : IEstrategiaImpuesto
{
    public string CodigoPais     => "GB";
    public string NombreImpuesto => "VAT (Reino Unido)";

    private const decimal TipoEstandar = 0.20m;
    private const decimal TipoReducido = 0.05m;

    // Tipo reducido
    private static readonly HashSet<string> CodigosReducidos =
        ["ENERG", "HYGIENE", "SANIT"];

    // Tipo 0% (tributan pero a tipo cero — diferente a exentos)
    private static readonly HashSet<string> CodigosTipoCero =
        ["ALIM", "LIBRO", "MEDIC", "PAN", "LECHE"];

    public ResultadoImpuesto Calcular(Factura factura)
    {
        var desglose      = new List<DesgloseTipo>();
        decimal totalImpuesto = 0m;

        foreach (var linea in factura.Lineas)
        {
            if (linea.EstaExento) continue;

            decimal tipo = ObtenerTipo(linea.CodigoProducto);
            decimal impuesto = linea.Subtotal * tipo;
            totalImpuesto += impuesto;

            string etiqueta = tipo switch
            {
                0m              => "Zero-rated",
                TipoReducido    => "Reduced rate",
                _               => "Standard rate"
            };

            desglose.Add(new DesgloseTipo(
                $"{linea.Descripcion} ({etiqueta})", tipo, impuesto));
        }

        return new ResultadoImpuesto
        {
            BaseImponible    = factura.BaseImponible,
            BaseExenta       = factura.BaseExenta,
            ImporteImpuesto  = totalImpuesto,
            TotalConImpuesto = factura.TotalBruto + totalImpuesto,
            NombreImpuesto   = NombreImpuesto,
            PaisAplicado     = "Reino Unido",
            Desglose         = desglose
        };
    }

    private static decimal ObtenerTipo(string codigo) =>
        CodigosReducidos.Contains(codigo) ? TipoReducido :
        CodigosTipoCero.Contains(codigo)  ? 0m           :
                                            TipoEstandar;
}