namespace Strategy.Facturacion.Factories;

using Strategy.Facturacion.Strategies;

/// <summary>
/// Fábrica que resuelve la estrategia correcta según el código ISO
/// del país, desacoplando la creación de estrategias del contexto.
/// </summary>
public class FabricaEstrategiaImpuesto
{
    private readonly Dictionary<string, Func<IEstrategiaImpuesto>> _estrategias;

    public FabricaEstrategiaImpuesto()
    {
        _estrategias = new Dictionary<string, Func<IEstrategiaImpuesto>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["ES"] = () => new EstrategiaImpuestoEspana(),
            ["DE"] = () => new EstrategiaImpuestoAlemania(),
            ["GB"] = () => new EstrategiaImpuestoReinoUnido(),
            ["US"] = () => new EstrategiaImpuestoUSA("CA"),
            ["US-NY"] = () => new EstrategiaImpuestoUSA("NY"),
            ["US-TX"] = () => new EstrategiaImpuestoUSA("TX"),
            ["US-OR"] = () => new EstrategiaImpuestoUSA("OR"),
        };
    }

    /// <summary>
    /// Resuelve la estrategia para el código de país dado.
    /// Si el país no está registrado, devuelve la estrategia sin impuesto.
    /// </summary>
    public IEstrategiaImpuesto ObtenerParaPais(string codigoPais)
    {
        if (_estrategias.TryGetValue(codigoPais, out var fabrica))
            return fabrica();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  ⚠ País '{codigoPais}' sin estrategia específica. " +
                           "Aplicando sin impuesto.");
        Console.ResetColor();

        return new EstrategiaImpuestoSinImpuesto();
    }

    /// <summary>
    /// Registra una nueva estrategia en tiempo de ejecución.
    /// Permite extender la fábrica sin modificarla (respeta OCP).
    /// </summary>
    public void RegistrarEstrategia(string codigoPais,
                                    Func<IEstrategiaImpuesto> fabrica)
        => _estrategias[codigoPais] = fabrica;

    public IReadOnlyList<string> PaisesRegistrados
        => _estrategias.Keys.ToList();
}