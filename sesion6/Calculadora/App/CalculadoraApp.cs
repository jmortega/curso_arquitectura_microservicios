using Calculadora.Exceptions;
using Calculadora.Services;

namespace Calculadora.App;

/// <summary>
/// Contiene toda la lógica de la aplicación de consola.
/// Separada de Program.cs para que Coverlet pueda instrumentarla
/// y para que sea testeable de forma independiente.
/// </summary>
public static class CalculadoraApp
{
    private static CalculadoraService _calculadora = new();
    private static HistorialService   _historial   = new();

    /// <summary>
    /// Punto de entrada principal de la aplicación.
    /// Se puede llamar desde Program.cs o desde tests de integración.
    /// </summary>
    public static void Ejecutar(string[] args)
    {
        // Permite inyectar instancias desde tests si se necesita
        _calculadora = new CalculadoraService();
        _historial   = new HistorialService();

        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║        CALCULADORA  .NET 8           ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.WriteLine();

        bool continuar = true;
        while (continuar)
        {
            MostrarMenu();
            switch (Console.ReadLine()?.Trim())
            {
                case "1": EjecutarBinaria("Suma",          (a, b) => _calculadora.Sumar(a, b));        break;
                case "2": EjecutarBinaria("Resta",         (a, b) => _calculadora.Restar(a, b));       break;
                case "3": EjecutarBinaria("Multiplicación",(a, b) => _calculadora.Multiplicar(a, b));  break;
                case "4": EjecutarBinaria("División",      (a, b) => _calculadora.Dividir(a, b));      break;
                case "5": EjecutarBinaria("Potencia",      (a, b) => _calculadora.Potencia(a, b));     break;
                case "6": EjecutarRaiz();                                                                break;
                case "7": EjecutarBinaria("Módulo",        (a, b) => _calculadora.Modulo(a, b));       break;
                case "8": EjecutarBinaria("Porcentaje",    (a, b) => _calculadora.Porcentaje(a, b));   break;
                case "9": MostrarHistorial();                                                            break;
                case "0": continuar = false;                                                             break;
                default:  Console.WriteLine("  Opción no válida.\n");                                   break;
            }
        }

        Console.WriteLine("\n¡Hasta pronto!");
    }

    // ─────────────────────────────────────────────────────────────
    // Operaciones de consola
    // ─────────────────────────────────────────────────────────────

    private static void MostrarMenu()
    {
        Console.WriteLine("┌────────────────────────────────────────┐");
        Console.WriteLine("│  1. Suma             5. Potencia        │");
        Console.WriteLine("│  2. Resta            6. Raíz cuadrada   │");
        Console.WriteLine("│  3. Multiplicación   7. Módulo          │");
        Console.WriteLine("│  4. División         8. Porcentaje      │");
        Console.WriteLine("│  9. Ver historial    0. Salir           │");
        Console.WriteLine("└────────────────────────────────────────┘");
        Console.Write("  Elige una opción: ");
    }

    private static void EjecutarBinaria(string nombre, Func<double, double, double> operacion)
    {
        try
        {
            Console.Write("\n  Primer número  : ");
            double a = LeerNumero();
            Console.Write("  Segundo número : ");
            double b = LeerNumero();

            double valor = operacion(a, b);
            Console.WriteLine($"\n  ✓ Resultado = {valor}\n");
            _historial.Registrar($"{nombre}({a}, {b})", valor);
        }
        catch (DivisionPorCeroException ex)   { MostrarError(ex.Message); }
        catch (DesbordamientoException  ex)   { MostrarError(ex.Message); }
        catch (ArgumentoInvalidoException ex) { MostrarError(ex.Message); }
    }

    private static void EjecutarRaiz()
    {
        try
        {
            Console.Write("\n  Número: ");
            double n    = LeerNumero();
            double raiz = _calculadora.RaizCuadrada(n);
            Console.WriteLine($"\n  ✓ √{n} = {raiz}\n");
            _historial.Registrar($"RaízCuadrada({n})", raiz);
        }
        catch (ArgumentoInvalidoException ex) { MostrarError(ex.Message); }
    }

    private static void MostrarHistorial()
    {
        Console.WriteLine("\n  ─── Historial ───");
        if (_historial.EstaVacio)
        {
            Console.WriteLine("  (sin operaciones)\n");
            return;
        }

        int i = 1;
        foreach (var e in _historial.Entradas)
            Console.WriteLine($"  {i++,2}. [{e.FechaHora:HH:mm:ss}]  {e.Descripcion} = {e.Resultado}");
        Console.WriteLine();
    }

    private static void MostrarError(string msg) =>
        Console.WriteLine($"\n  ✗ Error: {msg}\n");

    private static double LeerNumero()
    {
        while (true)
        {
            if (double.TryParse(
                    Console.ReadLine(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double n))
                return n;

            Console.Write("  Valor no válido, intenta de nuevo: ");
        }
    }
}