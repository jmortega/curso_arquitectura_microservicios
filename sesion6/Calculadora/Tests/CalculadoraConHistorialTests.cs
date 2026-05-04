using Calculadora.Exceptions;
using Calculadora.Services;
using FluentAssertions;
using Xunit;

namespace Calculadora.Tests;

/// <summary>
/// Tests de integración: CalculadoraService + HistorialService trabajando juntos.
/// Verifican que el flujo completo —calcular y registrar— funciona correctamente.
/// </summary>
public class CalculadoraConHistorialTests
{
    private readonly CalculadoraService _calculadora = new();
    private readonly HistorialService   _historial   = new();

    // Método auxiliar que simula lo que hace Program.cs
    private void EjecutarYRegistrar(string nombre, Func<double> operacion)
    {
        try
        {
            double resultado = operacion();
            _historial.Registrar($"{nombre} = {resultado}", resultado);
        }
        catch (Exception ex)
        {
            _historial.Registrar($"{nombre} → ERROR: {ex.Message}", double.NaN);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Flujo feliz
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void OperacionExitosa_SeRegistraEnHistorial()
    {
        EjecutarYRegistrar("Sumar(5, 3)", () => _calculadora.Sumar(5, 3));

        _historial.TotalOperaciones.Should().Be(1);
        _historial.Entradas[0].Resultado.Should().Be(8);
    }

    [Fact]
    public void VariasOperaciones_TodasQuedaranEnHistorialEnOrden()
    {
        EjecutarYRegistrar("Sumar",         () => _calculadora.Sumar(10, 5));
        EjecutarYRegistrar("Restar",        () => _calculadora.Restar(10, 5));
        EjecutarYRegistrar("Multiplicar",   () => _calculadora.Multiplicar(4, 3));
        EjecutarYRegistrar("Dividir",       () => _calculadora.Dividir(20, 4));

        _historial.TotalOperaciones.Should().Be(4);
        _historial.Entradas[0].Resultado.Should().Be(15);
        _historial.Entradas[1].Resultado.Should().Be(5);
        _historial.Entradas[2].Resultado.Should().Be(12);
        _historial.Entradas[3].Resultado.Should().Be(5);
    }

    // ─────────────────────────────────────────────────────────────
    // Operaciones con error se registran igual
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void DivisionPorCero_SeRegistraComoError()
    {
        EjecutarYRegistrar("Dividir(5, 0)", () => _calculadora.Dividir(5, 0));

        _historial.TotalOperaciones.Should().Be(1);
        _historial.Entradas[0].Descripcion.Should().Contain("ERROR");
        _historial.Entradas[0].Resultado.Should().Be(double.NaN);
    }

    [Fact]
    public void MezclaDeExitosYErrores_HistorialContieneAmbos()
    {
        EjecutarYRegistrar("Sumar(2, 3)",    () => _calculadora.Sumar(2, 3));        // ok
        EjecutarYRegistrar("Dividir(9, 0)",  () => _calculadora.Dividir(9, 0));      // error
        EjecutarYRegistrar("Restar(10, 4)",  () => _calculadora.Restar(10, 4));      // ok
        EjecutarYRegistrar("Raíz(-4)",       () => _calculadora.RaizCuadrada(-4));   // error

        _historial.TotalOperaciones.Should().Be(4);

        var exitosas = _historial.Entradas
            .Where(e => !double.IsNaN(e.Resultado))
            .ToList();
        var errores = _historial.Entradas
            .Where(e => double.IsNaN(e.Resultado))
            .ToList();

        exitosas.Should().HaveCount(2);
        errores.Should().HaveCount(2);
    }

    // ─────────────────────────────────────────────────────────────
    // Historial: últimas N operaciones
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void ObtenerUltimasDos_Tras5Operaciones_RetornaLasCorrectas()
    {
        for (int i = 1; i <= 5; i++)
            EjecutarYRegistrar($"Sumar({i}, 0)", () => _calculadora.Sumar(i, 0));

        var ultimas = _historial.ObtenerUltimas(2).ToList();

        ultimas.Should().HaveCount(2);
        ultimas[0].Resultado.Should().Be(4);   // 4ª operación
        ultimas[1].Resultado.Should().Be(5);   // 5ª operación
    }

    // ─────────────────────────────────────────────────────────────
    // Limpiar y continuar
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void LimpiarYContinuarOperando_HistorialSoloContieneNuevas()
    {
        EjecutarYRegistrar("Sumar(1, 1)", () => _calculadora.Sumar(1, 1));
        EjecutarYRegistrar("Sumar(2, 2)", () => _calculadora.Sumar(2, 2));

        _historial.Limpiar();

        EjecutarYRegistrar("Sumar(3, 3)", () => _calculadora.Sumar(3, 3));

        _historial.TotalOperaciones.Should().Be(1);
        _historial.Entradas[0].Resultado.Should().Be(6);
    }
}
