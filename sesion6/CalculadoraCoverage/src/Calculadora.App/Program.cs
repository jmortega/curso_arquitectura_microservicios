using Calculadora.Core.Exceptions;
using Calculadora.Core.Services;

var calculadora = new CalculadoraService();
var historial   = new HistorialService();

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
        case "1": EjecutarBinaria("Suma",          (a, b) => calculadora.Sumar(a, b));        break;
        case "2": EjecutarBinaria("Resta",         (a, b) => calculadora.Restar(a, b));       break;
        case "3": EjecutarBinaria("Multiplicación",(a, b) => calculadora.Multiplicar(a, b));  break;
        case "4": EjecutarBinaria("División",      (a, b) => calculadora.Dividir(a, b));      break;
        case "5": EjecutarBinaria("Potencia",      (a, b) => calculadora.Potencia(a, b));     break;
        case "6": EjecutarRaiz();                                                               break;
        case "7": EjecutarBinaria("Módulo",        (a, b) => calculadora.Modulo(a, b));       break;
        case "8": EjecutarBinaria("Porcentaje",    (a, b) => calculadora.Porcentaje(a, b));   break;
        case "9": MostrarHistorial();                                                           break;
        case "0": continuar = false;                                                            break;
        default:  Console.WriteLine("  Opción no válida.\n");                                  break;
    }
}

Console.WriteLine("\n¡Hasta pronto!");

void MostrarMenu()
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

void EjecutarBinaria(string nombre, Func<double, double, double> operacion)
{
    try
    {
        Console.Write("\n  Primer número  : ");
        double a = LeerNumero();
        Console.Write("  Segundo número : ");
        double b = LeerNumero();

        double valor = operacion(a, b);
        Console.WriteLine($"\n  ✓ Resultado = {valor}\n");
        historial.Registrar($"{nombre}({a}, {b})", valor);
    }
    catch (DivisionPorCeroException ex)   { MostrarError(ex.Message); }
    catch (DesbordamientoException  ex)   { MostrarError(ex.Message); }
    catch (ArgumentoInvalidoException ex) { MostrarError(ex.Message); }
}

void EjecutarRaiz()
{
    try
    {
        Console.Write("\n  Número: ");
        double n    = LeerNumero();
        double raiz = calculadora.RaizCuadrada(n);
        Console.WriteLine($"\n  ✓ √{n} = {raiz}\n");
        historial.Registrar($"RaízCuadrada({n})", raiz);
    }
    catch (ArgumentoInvalidoException ex) { MostrarError(ex.Message); }
}

void MostrarHistorial()
{
    Console.WriteLine("\n  ─── Historial ───");
    if (historial.EstaVacio) { Console.WriteLine("  (sin operaciones)\n"); return; }

    int i = 1;
    foreach (var e in historial.Entradas)
        Console.WriteLine($"  {i++,2}. [{e.FechaHora:HH:mm:ss}]  {e.Descripcion} = {e.Resultado}");
    Console.WriteLine();
}

void MostrarError(string msg) => Console.WriteLine($"\n  ✗ Error: {msg}\n");

double LeerNumero()
{
    while (true)
    {
        if (double.TryParse(Console.ReadLine(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double n))
            return n;
        Console.Write("  Valor no válido, intenta de nuevo: ");
    }
}
