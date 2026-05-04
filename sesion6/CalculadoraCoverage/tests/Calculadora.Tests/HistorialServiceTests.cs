using Calculadora.Core.Services;
using FluentAssertions;
using Xunit;

namespace Calculadora.Tests;

public class HistorialServiceTests
{
    private readonly HistorialService _sut = new();

    // ═══════════════════════════════════════════════════════════
    // Estado inicial
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void NuevoHistorial_EstaVacioYTotalEsCero()
    {
        _sut.EstaVacio.Should().BeTrue();
        _sut.TotalOperaciones.Should().Be(0);
        _sut.Entradas.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════
    // Registrar
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Registrar_OperacionValida_SeAgregaAlHistorial()
    {
        _sut.Registrar("Sumar(3, 4)", 7);

        _sut.TotalOperaciones.Should().Be(1);
        _sut.EstaVacio.Should().BeFalse();

        var entrada = _sut.Entradas[0];
        entrada.Descripcion.Should().Be("Sumar(3, 4)");
        entrada.Resultado.Should().Be(7);
        entrada.FechaHora.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Registrar_VariasOperaciones_TodasSeAgregan()
    {
        _sut.Registrar("Sumar(1, 2)",      3);
        _sut.Registrar("Restar(10, 4)",    6);
        _sut.Registrar("Multiplicar(3, 3)", 9);

        _sut.TotalOperaciones.Should().Be(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Registrar_DescripcionVaciaONula_LanzaArgumentException(
        string descripcionInvalida)
    {
        Action accion = () => _sut.Registrar(descripcionInvalida, 0);

        accion.Should().Throw<ArgumentException>()
              .WithParameterName("descripcion");
    }

    // ═══════════════════════════════════════════════════════════
    // ObtenerUltimas
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ObtenerUltimas_PideDosDeTres_RetornaLasDosUltimas()
    {
        _sut.Registrar("Op1", 1);
        _sut.Registrar("Op2", 2);
        _sut.Registrar("Op3", 3);

        var ultimas = _sut.ObtenerUltimas(2).ToList();

        ultimas.Should().HaveCount(2);
        ultimas[0].Descripcion.Should().Be("Op2");
        ultimas[1].Descripcion.Should().Be("Op3");
    }

    [Fact]
    public void ObtenerUltimas_PideMasDeLoQueHay_RetornaTodasLasDisponibles()
    {
        _sut.Registrar("Op1", 1);
        _sut.Registrar("Op2", 2);

        _sut.ObtenerUltimas(50).Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ObtenerUltimas_CantidadMenorOIgualACero_LanzaArgumentException(
        int cantidadInvalida)
    {
        Action accion = () => _sut.ObtenerUltimas(cantidadInvalida);

        accion.Should().Throw<ArgumentException>()
              .WithParameterName("cantidad");
    }

    // ═══════════════════════════════════════════════════════════
    // Limpiar
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Limpiar_ConEntradas_DejaElHistorialVacio()
    {
        _sut.Registrar("Op1", 10);
        _sut.Registrar("Op2", 20);

        _sut.Limpiar();

        _sut.EstaVacio.Should().BeTrue();
        _sut.TotalOperaciones.Should().Be(0);
    }

    [Fact]
    public void Limpiar_HistorialYaVacio_NoLanzaExcepcion()
    {
        Action accion = () => _sut.Limpiar();

        accion.Should().NotThrow();
    }
}
