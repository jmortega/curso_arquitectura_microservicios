using Calculadora.Core.Exceptions;
using Calculadora.Core.Services;
using FluentAssertions;
using Xunit;

namespace Calculadora.Tests;

/// <summary>
/// Tests de integración: CalculadoraService + HistorialService juntos.
/// </summary>
public class CalculadoraConHistorialTests
{
    private readonly CalculadoraService _calculadora = new();
    private readonly HistorialService   _historial   = new();

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
        EjecutarYRegistrar("Sumar",        () => _calculadora.Sumar(10, 5));
        EjecutarYRegistrar("Restar",       () => _calculadora.Restar(10, 5));
        EjecutarYRegistrar("Multiplicar",  () => _calculadora.Multiplicar(4, 3));
        EjecutarYRegistrar("Dividir",      () => _calculadora.Dividir(20, 4));

        _historial.TotalOperaciones.Should().Be(4);
        _historial.Entradas[0].Resultado.Should().Be(15);
        _historial.Entradas[1].Resultado.Should().Be(5);
        _historial.Entradas[2].Resultado.Should().Be(12);
        _historial.Entradas[3].Resultado.Should().Be(5);
    }

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
        EjecutarYRegistrar("Sumar(2, 3)",    () => _calculadora.Sumar(2, 3));
        EjecutarYRegistrar("Dividir(9, 0)",  () => _calculadora.Dividir(9, 0));
        EjecutarYRegistrar("Restar(10, 4)",  () => _calculadora.Restar(10, 4));
        EjecutarYRegistrar("Raíz(-4)",       () => _calculadora.RaizCuadrada(-4));

        var exitosas = _historial.Entradas.Where(e => !double.IsNaN(e.Resultado)).ToList();
        var errores  = _historial.Entradas.Where(e =>  double.IsNaN(e.Resultado)).ToList();

        exitosas.Should().HaveCount(2);
        errores.Should().HaveCount(2);
    }

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
