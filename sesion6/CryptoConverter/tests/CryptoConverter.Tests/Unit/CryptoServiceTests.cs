using CryptoConverter.API.Exceptions;
using CryptoConverter.API.Models;
using CryptoConverter.API.Services;
using CryptoConverter.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using System.Net;
using Xunit;

namespace CryptoConverter.Tests.Unit;

/// <summary>
/// Tests unitarios de CryptoService.
/// Usa MockHttpMessageHandler.Fallback para evitar el problema de
/// URL-encoding de wildcards en query strings de MockHttp v7.
/// </summary>
public class CryptoServiceTests
{
    // ─── Helper: crea el servicio con un HttpClient directo del mock ───
    // Sin IHttpClientFactory ni Polly, igual que FunctionalWebApplicationFactory.
    private static CryptoService CrearServicio(MockHttpMessageHandler mockHttp)
    {
        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CryptoConverter/1.0 (test)");
        return new CryptoService(client, NullLogger<CryptoService>.Instance);
    }

    // ─── Helper: mock que responde con éxito a CUALQUIER petición ─────
    private static MockHttpMessageHandler MockExitoso(string json)
    {
        var mock = new MockHttpMessageHandler();
        mock.Fallback.Respond("application/json", json);
        return mock;
    }

    // ─── Helper: mock que responde con un error HTTP a CUALQUIER petición
    private static MockHttpMessageHandler MockError(HttpStatusCode codigo)
    {
        var mock = new MockHttpMessageHandler();
        mock.Fallback.Respond(codigo);
        return mock;
    }

    // ═══════════════════════════════════════════════════════════
    // ObtenerPrecioAsync — precio individual
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ObtenerPrecioAsync_Bitcoin_RetornaPrecioCorrectoEnEuros()
    {
        var sut = CrearServicio(MockExitoso(CoinGeckoResponses.SoloBitcoin));

        var precio = await sut.ObtenerPrecioAsync(CryptoMoneda.BTC);

        precio.Simbolo.Should().Be("BTC");
        precio.Nombre.Should().Be("Bitcoin");
        precio.PrecioEur.Should().Be(58000.50m);
        precio.Variacion24hPct.Should().Be(2.35m);
        precio.UltimaActualizacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ObtenerPrecioAsync_Ethereum_RetornaPrecioCorrectoEnEuros()
    {
        var sut = CrearServicio(MockExitoso(CoinGeckoResponses.SoloEthereum));

        var precio = await sut.ObtenerPrecioAsync(CryptoMoneda.ETH);

        precio.Simbolo.Should().Be("ETH");
        precio.PrecioEur.Should().Be(3100.75m);
        precio.Variacion24hPct.Should().Be(-1.20m);
    }

    [Theory]
    [InlineData(CryptoMoneda.BTC, "Bitcoin",       58000.50,  2.35)]
    [InlineData(CryptoMoneda.ETH, "Ethereum",       3100.75, -1.20)]
    [InlineData(CryptoMoneda.BNB, "Binance Coin",    520.30,  0.85)]
    [InlineData(CryptoMoneda.SOL, "Solana",           145.60,  4.10)]
    [InlineData(CryptoMoneda.ADA, "Cardano",            0.45, -0.30)]
    public async Task ObtenerPrecioAsync_TodasLasMonedas_RetornanNombreYPrecio(
        CryptoMoneda moneda, string nombreEsperado, double precioEsperado, double variacionEsperada)
    {
        // Fallback devuelve siempre TodosLosPrecios; CryptoService filtra la moneda correcta
        var sut = CrearServicio(MockExitoso(CoinGeckoResponses.TodosLosPrecios));

        var precio = await sut.ObtenerPrecioAsync(moneda);

        precio.Nombre.Should().Be(nombreEsperado);
        precio.PrecioEur.Should().Be((decimal)precioEsperado);
        precio.Variacion24hPct.Should().Be((decimal)variacionEsperada);
    }

    [Fact]
    public async Task ObtenerPrecioAsync_CoinGeckoDevuelveError429_LanzaPrecioNoDisponibleException()
    {
        var sut = CrearServicio(MockError(HttpStatusCode.TooManyRequests));

        Func<Task> accion = () => sut.ObtenerPrecioAsync(CryptoMoneda.BTC);

        await accion.Should().ThrowAsync<PrecioNoDisponibleException>()
            .WithMessage("*BTC*");
    }

    [Fact]
    public async Task ObtenerPrecioAsync_CoinGeckoDevuelve503_LanzaPrecioNoDisponibleException()
    {
        var sut = CrearServicio(MockError(HttpStatusCode.ServiceUnavailable));

        Func<Task> accion = () => sut.ObtenerPrecioAsync(CryptoMoneda.ETH);

        await accion.Should().ThrowAsync<PrecioNoDisponibleException>()
            .WithMessage("*ETH*");
    }

    [Fact]
    public async Task ObtenerPrecioAsync_RespuestaJsonVacia_LanzaPrecioNoDisponibleException()
    {
        var sut = CrearServicio(MockExitoso("{}"));

        Func<Task> accion = () => sut.ObtenerPrecioAsync(CryptoMoneda.BTC);

        await accion.Should().ThrowAsync<PrecioNoDisponibleException>();
    }

    // ═══════════════════════════════════════════════════════════
    // ConvertirAsync
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ConvertirAsync_1000EurosABitcoin_RetornaCantidadCorrecta()
    {
        // Bitcoin a 58000.50€ → 1000 / 58000.50 ≈ 0.01724137
        var sut = CrearServicio(MockExitoso(CoinGeckoResponses.SoloBitcoin));

        var resultado = await sut.ConvertirAsync(1000m, CryptoMoneda.BTC);

        resultado.Euros.Should().Be(1000m);
        resultado.Moneda.Should().Be("BTC");
        resultado.PrecioUnitarioEur.Should().Be(58000.50m);
        resultado.CantidadCrypto.Should().BeApproximately(0.01724m, precision: 0.0001m);
        resultado.Variacion24hPct.Should().Be(2.35m);
    }

    [Fact]
    public async Task ConvertirAsync_1000EurosAEthereum_RetornaCantidadCorrecta()
    {
        // Ethereum a 3100.75€ → 1000 / 3100.75 ≈ 0.32249
        var sut = CrearServicio(MockExitoso(CoinGeckoResponses.SoloEthereum));

        var resultado = await sut.ConvertirAsync(1000m, CryptoMoneda.ETH);

        resultado.CantidadCrypto.Should().BeApproximately(0.32249m, precision: 0.0001m);
        resultado.Variacion24hPct.Should().Be(-1.20m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.001)]
    public async Task ConvertirAsync_EurosMenorOIgualACero_LanzaCantidadInvalidaException(decimal euros)
    {
        // No llega a hacer HTTP — falla antes de la validación
        var sut = CrearServicio(new MockHttpMessageHandler());

        Func<Task> accion = () => sut.ConvertirAsync(euros, CryptoMoneda.BTC);

        await accion.Should().ThrowAsync<CantidadInvalidaException>()
            .WithMessage("*mayor que 0*");
    }

    [Fact]
    public async Task ConvertirAsync_ResultadoTieneMaximoOchoDecimales()
    {
        // 1 euro a Cardano (0.45€/ADA) ≈ 2.22 ADA
        var sut = CrearServicio(MockExitoso(CoinGeckoResponses.SoloCardano));

        var resultado = await sut.ConvertirAsync(1m, CryptoMoneda.ADA);

        var decimales = resultado.CantidadCrypto.ToString().Contains('.')
            ? resultado.CantidadCrypto.ToString().Split('.')[1].TrimEnd('0').Length
            : 0;
        decimales.Should().BeLessThanOrEqualTo(8);
    }

    // ═══════════════════════════════════════════════════════════
    // ObtenerTodosLosPreciosAsync
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ObtenerTodosLosPreciosAsync_RetornaCincoMonedasEnUnaLlamada()
    {
        var sut = CrearServicio(MockExitoso(CoinGeckoResponses.TodosLosPrecios));

        var respuesta = await sut.ObtenerTodosLosPreciosAsync();

        respuesta.Precios.Should().HaveCount(5);
        respuesta.Precios.Select(p => p.Simbolo)
            .Should().Contain(new[] { "BTC", "ETH", "BNB", "SOL", "ADA" });
    }

    [Fact]
    public async Task ObtenerTodosLosPreciosAsync_TodosLosPreciosPositivos()
    {
        var sut = CrearServicio(MockExitoso(CoinGeckoResponses.TodosLosPrecios));

        var respuesta = await sut.ObtenerTodosLosPreciosAsync();

        respuesta.Precios.Should().AllSatisfy(p => p.PrecioEur.Should().BePositive());
    }

    [Fact]
    public async Task ObtenerTodosLosPreciosAsync_PreciosCoincideConValoresMockeados()
    {
        var sut = CrearServicio(MockExitoso(CoinGeckoResponses.TodosLosPrecios));

        var respuesta = await sut.ObtenerTodosLosPreciosAsync();

        respuesta.Precios.Single(p => p.Simbolo == "BTC").PrecioEur.Should().Be(58000.50m);
        respuesta.Precios.Single(p => p.Simbolo == "ETH").PrecioEur.Should().Be(3100.75m);
        respuesta.Precios.Single(p => p.Simbolo == "ADA").PrecioEur.Should().Be(0.45m);
    }

    [Fact]
    public async Task ObtenerTodosLosPreciosAsync_CoinGeckoFalla_LanzaPrecioNoDisponibleException()
    {
        var sut = CrearServicio(MockError(HttpStatusCode.TooManyRequests));

        Func<Task> accion = () => sut.ObtenerTodosLosPreciosAsync();

        await accion.Should().ThrowAsync<PrecioNoDisponibleException>();
    }
}