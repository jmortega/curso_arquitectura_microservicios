namespace Strategy.Facturacion.Strategies;

using Strategy.Facturacion.Models;

/// <summary>
/// IVA español con tres tipos: general (21%), reducido (10%)
/// y superreducido (4%) según el código de producto.
/// Los productos con EstaExento = true tributan al 0%.
/// </summary>
public class EstrategiaImpuestoEspana : IEstrategiaImpuesto
{
    public string CodigoPais    => "ES";
    public string NombreImpuesto => "IVA (España)";

    private const decimal TipoGeneral      = 0.21m;
    private const decimal TipoReducido     = 0.10m;
    private const decimal TipoSuperReducido = 0.04m;

    // Productos con tipo reducido (alimentación, hostelería, transporte...)
    private static readonly HashSet<string> CodigosReducidos =
        ["ALIM", "HOSTEL", "TRANS", "FARM"];

    // Productos con tipo superreducido (pan, leche, libros, medicamentos...)
    private static readonly HashSet<string> CodigosSuperReducidos =
        ["PAN", "LECHE", "LIBRO", "MEDIC"];

    public ResultadoImpuesto Calcular(Factura factura)
    {
        var desglose = new List<DesgloseTipo>();
        decimal totalImpuesto = 0m;

        foreach (var linea in factura.Lineas.Where(l => !l.EstaExento))
        {
            decimal tipo = ObtenerTipo(linea.CodigoProducto);
            decimal impuesto = linea.Subtotal * tipo;
            totalImpuesto += impuesto;

            string descripcion = tipo switch
            {
                TipoSuperReducido => $"{linea.Descripcion} (superreducido)",
                TipoReducido      => $"{linea.Descripcion} (reducido)",
                _                 => $"{linea.Descripcion} (general)"
            };

            desglose.Add(new DesgloseTipo(descripcion, tipo, impuesto));
        }

        return new ResultadoImpuesto
        {
            BaseImponible    = factura.BaseImponible,
            BaseExenta       = factura.BaseExenta,
            ImporteImpuesto  = totalImpuesto,
            TotalConImpuesto = factura.TotalBruto + totalImpuesto,
            NombreImpuesto   = NombreImpuesto,
            PaisAplicado     = "España",
            Desglose         = desglose
        };
    }

    private static decimal ObtenerTipo(string codigoProducto) =>
        CodigosSuperReducidos.Contains(codigoProducto) ? TipoSuperReducido :
        CodigosReducidos.Contains(codigoProducto)      ? TipoReducido      :
                                                         TipoGeneral;
}