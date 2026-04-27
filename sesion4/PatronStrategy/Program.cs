// ── Program.cs ────────────────────────────────────────────────────────
using Strategy.Facturacion.Context;
using Strategy.Facturacion.Factories;
using Strategy.Facturacion.Models;
using Strategy.Facturacion.Strategies;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ── Factura de ejemplo ────────────────────────────────────────────────
var factura = new Factura
{
    ClienteNombre = "Empresa Internacional S.L.",
    ClienteEmail  = "empresa@ejemplo.com",
    PaisIso       = "ES",
    Lineas        =
    [
        new LineaFactura
        {
            Descripcion    = "Laptop Pro 15",
            Cantidad       = 2,
            PrecioUnitario = 1_200.00m,
            CodigoProducto = "TECH"          // Tipo general
        },
        new LineaFactura
        {
            Descripcion    = "Pack alimentación",
            Cantidad       = 10,
            PrecioUnitario = 15.00m,
            CodigoProducto = "ALIM"          // Tipo reducido / 0% según país
        },
        new LineaFactura
        {
            Descripcion    = "Libro técnico C#",
            Cantidad       = 5,
            PrecioUnitario = 35.00m,
            CodigoProducto = "LIBRO"         // Tipo superreducido / 0% según país
        },
        new LineaFactura
        {
            Descripcion    = "Servicio de consultoría",
            Cantidad       = 1,
            PrecioUnitario = 500.00m,
            CodigoProducto = "SERV",
            EstaExento     = true           // Exento en todos los países
        }
    ]
};

MostrarFacturaBase(factura);

// ── Demostración del patrón Strategy ─────────────────────────────────
var fabrica  = new FabricaEstrategiaImpuesto();
var contexto = new ContextoFacturacion(fabrica.ObtenerParaPais("ES"));

// Calcular con España
Console.WriteLine("\n══════════════════════════════════════════");
Console.WriteLine("  CÁLCULO CON ESTRATEGIA: ESPAÑA");
Console.WriteLine("══════════════════════════════════════════");
contexto.CalcularImpuesto(factura).Mostrar();

// Cambiar estrategia a Alemania en tiempo de ejecución
contexto.CambiarEstrategia(fabrica.ObtenerParaPais("DE"));
Console.WriteLine("\n══════════════════════════════════════════");
Console.WriteLine("  CÁLCULO CON ESTRATEGIA: ALEMANIA");
Console.WriteLine("══════════════════════════════════════════");
contexto.CalcularImpuesto(factura).Mostrar();

// Cambiar a Reino Unido
contexto.CambiarEstrategia(fabrica.ObtenerParaPais("GB"));
Console.WriteLine("\n══════════════════════════════════════════");
Console.WriteLine("  CÁLCULO CON ESTRATEGIA: REINO UNIDO");
Console.WriteLine("══════════════════════════════════════════");
contexto.CalcularImpuesto(factura).Mostrar();

// Cambiar a EEUU - California
contexto.CambiarEstrategia(fabrica.ObtenerParaPais("US"));
Console.WriteLine("\n══════════════════════════════════════════");
Console.WriteLine("  CÁLCULO CON ESTRATEGIA: EEUU (California)");
Console.WriteLine("══════════════════════════════════════════");
contexto.CalcularImpuesto(factura).Mostrar();

// Cambiar a EEUU - Oregon (sin Sales Tax)
contexto.CambiarEstrategia(fabrica.ObtenerParaPais("US-OR"));
Console.WriteLine("\n══════════════════════════════════════════");
Console.WriteLine("  CÁLCULO CON ESTRATEGIA: EEUU (Oregon)");
Console.WriteLine("══════════════════════════════════════════");
contexto.CalcularImpuesto(factura).Mostrar();

// País sin estrategia registrada — devuelve Null Object
contexto.CambiarEstrategia(fabrica.ObtenerParaPais("JP"));
Console.WriteLine("\n══════════════════════════════════════════");
Console.WriteLine("  CÁLCULO CON ESTRATEGIA: JAPÓN (no registrado)");
Console.WriteLine("══════════════════════════════════════════");
contexto.CalcularImpuesto(factura).Mostrar();

// Registrar nueva estrategia en tiempo de ejecución (OCP)
Console.WriteLine("\n══════════════════════════════════════════");
Console.WriteLine("  REGISTRANDO NUEVA ESTRATEGIA: FRANCIA");
Console.WriteLine("══════════════════════════════════════════");
fabrica.RegistrarEstrategia("FR", () => new EstrategiaImpuestoPersonalizada(
    codigoPais:     "FR",
    nombreImpuesto: "TVA (Francia)",
    nombrePais:     "Francia",
    tipoGeneral:    0.20m));

contexto.CambiarEstrategia(fabrica.ObtenerParaPais("FR"));
contexto.CalcularImpuesto(factura).Mostrar();

// ── Helpers de presentación ───────────────────────────────────────────
static void MostrarFacturaBase(Factura f)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"""

        ╔══════════════════════════════════════════════════════╗
          FACTURA BASE
          Cliente:  {f.ClienteNombre}
          Email:    {f.ClienteEmail}
          Fecha:    {f.Fecha:dd/MM/yyyy HH:mm}
        ╠══════════════════════════════════════════════════════╣
        """);

    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine($"  {"Descripción",-28} {"Cód",-8} {"Cant",4} {"Precio",10} {"Subtotal",10} {"Exento",7}");
    Console.WriteLine($"  {"─────────────────────────────────────────────────────────────────"}");

    foreach (var l in f.Lineas)
        Console.WriteLine($"  {l.Descripcion,-28} {l.CodigoProducto,-8} {l.Cantidad,4} " +
                          $"{l.PrecioUnitario,10:C2} {l.Subtotal,10:C2} {(l.EstaExento ? "Sí" : "No"),7}");

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"""
        ╠══════════════════════════════════════════════════════╣
          Total bruto (sin impuestos):   {f.TotalBruto,10:C2}
          Base imponible:                {f.BaseImponible,10:C2}
          Base exenta:                   {f.BaseExenta,10:C2}
        ╚══════════════════════════════════════════════════════╝
        """);
    Console.ResetColor();
}