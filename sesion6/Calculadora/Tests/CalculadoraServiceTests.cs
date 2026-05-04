using Calculadora.Exceptions;
using Calculadora.Services;
using FluentAssertions;
using Xunit;

namespace Calculadora.Tests;

/// <summary>
/// Tests unitarios de CalculadoraService.
/// Cada método público tiene su propia región con tests del camino
/// feliz, caminos de error y casos límite.
/// </summary>
public class CalculadoraServiceTests
{
    // La instancia se crea de nuevo antes de cada test (xUnit instancia
    // la clase una vez por test, no hay estado compartido entre tests).
    private readonly CalculadoraService _sut = new();

    // ═══════════════════════════════════════════════════════════════════
    // SUMAR
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Sumar_DosPositivos_RetornaSumaCorrecta()
    {
        // Arrange
        double a = 3, b = 4;

        // Act
        double resultado = _sut.Sumar(a, b);

        // Assert
        resultado.Should().Be(7);
    }

    [Fact]
    public void Sumar_NumeroNegativoYPositivo_RetornaValorCorrecto()
    {
        _sut.Sumar(-5, 3).Should().Be(-2);
    }

    [Fact]
    public void Sumar_DosNegativos_RetornaNegativo()
    {
        _sut.Sumar(-4, -6).Should().Be(-10);
    }

    [Fact]
    public void Sumar_ConCero_RetornaMismoNumero()
    {
        _sut.Sumar(99, 0).Should().Be(99);
    }

    [Theory]
    [InlineData(2.5,   1.5,  4.0)]
    [InlineData(0.1,   0.2,  0.3)]   // precisión de punto flotante
    [InlineData(-3.0,  3.0,  0.0)]
    [InlineData(100.0, 0.01, 100.01)]
    public void Sumar_Decimales_RetornaValorAproximado(double a, double b, double esperado)
    {
        _sut.Sumar(a, b).Should().BeApproximately(esperado, precision: 1e-9);
    }

    [Fact]
    public void Sumar_ResultadoSuperaLimite_LanzaDesbordamientoException()
    {
        double enorme = 1_000_000_000_000;

        Action accion = () => _sut.Sumar(enorme, enorme);

        accion.Should().Throw<DesbordamientoException>()
              .WithMessage("*Sumar*");
    }

    // ═══════════════════════════════════════════════════════════════════
    // RESTAR
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Restar_DosPositivos_RetornaDiferenciaCorrecta()
    {
        _sut.Restar(10, 3).Should().Be(7);
    }

    [Fact]
    public void Restar_NumeroMenorDeNumeroMayor_RetornaNegativo()
    {
        _sut.Restar(3, 10).Should().Be(-7);
    }

    [Fact]
    public void Restar_MismoNumero_RetornaCero()
    {
        _sut.Restar(5, 5).Should().Be(0);
    }

    [Theory]
    [InlineData(10.0,  3.0,  7.0)]
    [InlineData(0.0,   5.0, -5.0)]
    [InlineData(-2.0, -8.0,  6.0)]
    public void Restar_VariosEscenarios_RetornaValorCorrecto(
        double a, double b, double esperado)
    {
        _sut.Restar(a, b).Should().BeApproximately(esperado, precision: 1e-9);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MULTIPLICAR
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Multiplicar_DosPositivos_RetornaProductoCorrecto()
    {
        _sut.Multiplicar(6, 7).Should().Be(42);
    }

    [Fact]
    public void Multiplicar_PorCero_RetornaCero()
    {
        _sut.Multiplicar(999, 0).Should().Be(0);
    }

    [Fact]
    public void Multiplicar_PorUno_RetornaMismoNumero()
    {
        _sut.Multiplicar(15, 1).Should().Be(15);
    }

    [Fact]
    public void Multiplicar_NegativoPorPositivo_RetornaNegativo()
    {
        _sut.Multiplicar(-3, 5).Should().Be(-15);
    }

    [Fact]
    public void Multiplicar_DosNegativos_RetornaPositivo()
    {
        _sut.Multiplicar(-4, -4).Should().Be(16);
    }

    [Fact]
    public void Multiplicar_ResultadoSuperaLimite_LanzaDesbordamientoException()
    {
        double enorme = 1_000_000_000;

        Action accion = () => _sut.Multiplicar(enorme, enorme);

        accion.Should().Throw<DesbordamientoException>();
    }

    // ═══════════════════════════════════════════════════════════════════
    // DIVIDIR
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Dividir_DivisionExacta_RetornaCocienteCorrecto()
    {
        _sut.Dividir(10, 2).Should().Be(5);
    }

    [Fact]
    public void Dividir_DivisionNoExacta_RetornaDecimal()
    {
        _sut.Dividir(10, 3).Should().BeApproximately(3.333, precision: 0.001);
    }

    [Fact]
    public void Dividir_NumeradorCero_RetornaCero()
    {
        _sut.Dividir(0, 5).Should().Be(0);
    }

    [Fact]
    public void Dividir_NegativoEntrePosistivo_RetornaNegativo()
    {
        _sut.Dividir(-20, 4).Should().Be(-5);
    }

    [Fact]
    public void Dividir_EntreNegativo_InvierteSigno()
    {
        _sut.Dividir(20, -4).Should().Be(-5);
    }

    /// <summary>
    /// El caso más importante de la división: divisor cero.
    /// </summary>
    [Fact]
    public void Dividir_DivisorCero_LanzaDivisionPorCeroException()
    {
        Action accion = () => _sut.Dividir(10, 0);

        accion.Should().Throw<DivisionPorCeroException>()
              .WithMessage("*dividir entre cero*");
    }

    [Fact]
    public void Dividir_NumeradorYDivisorCero_LanzaDivisionPorCeroException()
    {
        Action accion = () => _sut.Dividir(0, 0);

        accion.Should().Throw<DivisionPorCeroException>();
    }

    // ═══════════════════════════════════════════════════════════════════
    // POTENCIA
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(2,  3,  8)]
    [InlineData(5,  2,  25)]
    [InlineData(10, 0,  1)]     // cualquier número elevado a 0 = 1
    [InlineData(0,  5,  0)]     // 0 elevado a cualquier positivo = 0
    [InlineData(1,  99, 1)]     // 1 elevado a cualquier número = 1
    public void Potencia_CasosEstandar_RetornaValorCorrecto(
        double baseNum, double exp, double esperado)
    {
        _sut.Potencia(baseNum, exp).Should().BeApproximately(esperado, precision: 1e-9);
    }

    [Fact]
    public void Potencia_ExponenteNegativo_RetornaFraccion()
    {
        // 2^-1 = 0.5
        _sut.Potencia(2, -1).Should().BeApproximately(0.5, precision: 1e-9);
    }

    [Fact]
    public void Potencia_ExponenteDecimal_RetornaRaiz()
    {
        // 9^0.5 = √9 = 3
        _sut.Potencia(9, 0.5).Should().BeApproximately(3.0, precision: 1e-9);
    }

    // ═══════════════════════════════════════════════════════════════════
    // RAÍZ CUADRADA
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(4,   2)]
    [InlineData(9,   3)]
    [InlineData(100, 10)]
    [InlineData(0,   0)]
    [InlineData(2,   1.41421)]
    public void RaizCuadrada_NumeroPositivo_RetornaRaizCorrecta(
        double numero, double esperado)
    {
        _sut.RaizCuadrada(numero).Should().BeApproximately(esperado, precision: 0.0001);
    }

    [Fact]
    public void RaizCuadrada_Cero_RetornaCero()
    {
        _sut.RaizCuadrada(0).Should().Be(0);
    }

    [Fact]
    public void RaizCuadrada_NumeroNegativo_LanzaArgumentoInvalidoException()
    {
        Action accion = () => _sut.RaizCuadrada(-9);

        accion.Should().Throw<ArgumentoInvalidoException>()
              .WithMessage("*negativo*");
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-100)]
    [InlineData(-999999)]
    public void RaizCuadrada_CualquierNegativo_SiempreLanzaExcepcion(double numero)
    {
        Action accion = () => _sut.RaizCuadrada(numero);
        accion.Should().Throw<ArgumentoInvalidoException>();
    }

    // ═══════════════════════════════════════════════════════════════════
    // MÓDULO
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(10,  3,  1)]
    [InlineData(10,  2,  0)]    // división exacta: resto 0
    [InlineData(7,   7,  0)]    // mismo número: resto 0
    [InlineData(5,  10,  5)]    // divisor mayor que dividendo
    [InlineData(-7,  3, -1)]    // negativos siguen la convención de C#
    public void Modulo_VariosEscenarios_RetornaRestoCorrect(
        double a, double b, double esperado)
    {
        _sut.Modulo(a, b).Should().Be(esperado);
    }

    [Fact]
    public void Modulo_DivisorCero_LanzaDivisionPorCeroException()
    {
        Action accion = () => _sut.Modulo(10, 0);

        accion.Should().Throw<DivisionPorCeroException>()
              .WithMessage("*módulo*");
    }

    // ═══════════════════════════════════════════════════════════════════
    // PORCENTAJE
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(200, 10,  20)]    // 10% de 200 = 20
    [InlineData(500, 50, 250)]    // 50% de 500 = 250
    [InlineData(100, 100, 100)]   // 100% de 100 = 100
    [InlineData(100,  0,   0)]    // 0% de cualquier número = 0
    [InlineData(0,   25,   0)]    // porcentaje de cero = cero
    public void Porcentaje_VariosEscenarios_RetornaValorCorrecto(
        double valor, double pct, double esperado)
    {
        _sut.Porcentaje(valor, pct).Should().BeApproximately(esperado, precision: 1e-9);
    }

    [Fact]
    public void Porcentaje_PorcentajeNegativo_LanzaArgumentoInvalidoException()
    {
        Action accion = () => _sut.Porcentaje(100, -10);

        accion.Should().Throw<ArgumentoInvalidoException>()
              .WithMessage("*negativo*");
    }

    [Fact]
    public void Porcentaje_MayorQueCien_EsPermitido()
    {
        // 150% de 200 = 300
        _sut.Porcentaje(200, 150).Should().Be(300);
    }
}
