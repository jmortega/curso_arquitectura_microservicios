using Calculadora.Exceptions;
using Calculadora.Interfaces;

namespace Calculadora.Services;

/// <summary>
/// Implementa todas las operaciones matemáticas de la calculadora.
/// </summary>
public class CalculadoraService : ICalculadora
{
    private const double ValorMaximo = 1_000_000_000_000;

    // ─────────────────────────────────────────────────────────────
    // Operaciones básicas
    // ─────────────────────────────────────────────────────────────

    /// <summary>Suma dos números reales.</summary>
    public double Sumar(double a, double b)
    {
        var resultado = a + b;
        ValidarRango(resultado, nameof(Sumar));
        return resultado;
    }

    /// <summary>Resta b a a.</summary>
    public double Restar(double a, double b)
    {
        var resultado = a - b;
        ValidarRango(resultado, nameof(Restar));
        return resultado;
    }

    /// <summary>Multiplica dos números reales.</summary>
    public double Multiplicar(double a, double b)
    {
        var resultado = a * b;
        ValidarRango(resultado, nameof(Multiplicar));
        return resultado;
    }

    /// <summary>Divide a entre b.</summary>
    /// <exception cref="DivisionPorCeroException">Si b es cero.</exception>
    public double Dividir(double a, double b)
    {
        if (b == 0)
            throw new DivisionPorCeroException();

        var resultado = a / b;
        ValidarRango(resultado, nameof(Dividir));
        return resultado;
    }

    // ─────────────────────────────────────────────────────────────
    // Operaciones avanzadas
    // ─────────────────────────────────────────────────────────────

    /// <summary>Calcula baseNum elevado al exponente.</summary>
    public double Potencia(double baseNum, double exponente)
    {
        var resultado = Math.Pow(baseNum, exponente);

        if (double.IsNaN(resultado))
            throw new ArgumentoInvalidoException(
                nameof(baseNum),
                "La operación de potencia no está definida para estos valores.");

        ValidarRango(resultado, nameof(Potencia));
        return resultado;
    }

    /// <summary>Calcula la raíz cuadrada de un número.</summary>
    /// <exception cref="ArgumentoInvalidoException">Si el número es negativo.</exception>
    public double RaizCuadrada(double numero)
    {
        if (numero < 0)
            throw new ArgumentoInvalidoException(
                nameof(numero),
                $"No se puede calcular la raíz cuadrada de un número negativo ({numero}).");

        return Math.Sqrt(numero);
    }

    /// <summary>Calcula el resto de la división entera (módulo).</summary>
    /// <exception cref="DivisionPorCeroException">Si el divisor es cero.</exception>
    public double Modulo(double a, double b)
    {
        if (b == 0)
            throw new DivisionPorCeroException("El divisor del módulo no puede ser cero.");

        return a % b;
    }

    /// <summary>Calcula (valor * porcentaje) / 100.</summary>
    /// <exception cref="ArgumentoInvalidoException">Si el porcentaje es negativo.</exception>
    public double Porcentaje(double valor, double porcentaje)
    {
        if (porcentaje < 0)
            throw new ArgumentoInvalidoException(
                nameof(porcentaje),
                $"El porcentaje no puede ser negativo ({porcentaje}).");

        return (valor * porcentaje) / 100.0;
    }

    // ─────────────────────────────────────────────────────────────
    // Validación interna
    // ─────────────────────────────────────────────────────────────

    private static void ValidarRango(double valor, string operacion)
    {
        if (Math.Abs(valor) > ValorMaximo)
            throw new DesbordamientoException(
                $"El resultado de '{operacion}' ({valor:G}) supera el límite ±{ValorMaximo:G}.");
    }
}
