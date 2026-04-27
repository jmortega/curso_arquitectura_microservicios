namespace Strategy.Facturacion.Strategies;

using Strategy.Facturacion.Models;

/// <summary>
/// IVA alemán (Mehrwertsteuer): tipo general 19%, reducido 7%
/// para alimentos básicos, libros, transporte y cultura.
/// </summary>
public class EstrategiaImpuestoAlemania : IEstrategiaImpuesto
{
    public string CodigoPais     => "DE";
    public string NombreImpuesto => "MwSt (Alemania)";

    private const decimal TipoGeneral  = 0.19m;
    private const decimal TipoReducido = 0.07m;

    private static readonly HashSet<string> CodigosReducidos =
        ["ALIM", "LIBRO", "TRANS", "CULT", "PAN", "LECHE"];

    public ResultadoImpuesto Calcular(Factura factura)
    {
        var desglose      = new List<DesgloseTipo>();
        decimal totalImpuesto = 0m;

        foreach (var linea in factura.Lineas.Where(l => !l.EstaExento))
        {
            bool esReducido   = CodigosReducidos.Contains(linea.CodigoProducto);
            decimal tipo      = esReducido ? TipoReducido : TipoGeneral;
            decimal impuesto  = linea.Subtotal * tipo;
            totalImpuesto    += impuesto;

            desglose.Add(new DesgloseTipo(
                $"{linea.Descripcion} ({(esReducido ? "ermäßigt" : "allgemein")})",
                tipo,
                impuesto));
        }

        return new ResultadoImpuesto
        {
            BaseImponible    = factura.BaseImponible,
            BaseExenta       = factura.BaseExenta,
            ImporteImpuesto  = totalImpuesto,
            TotalConImpuesto = factura.TotalBruto + totalImpuesto,
            NombreImpuesto   = NombreImpuesto,
            PaisAplicado     = "Alemania",
            Desglose         = desglose
        };
    }
}