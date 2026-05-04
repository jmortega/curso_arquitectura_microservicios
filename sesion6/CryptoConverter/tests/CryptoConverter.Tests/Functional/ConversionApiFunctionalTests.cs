using CryptoConverter.API.Models;
using CryptoConverter.Tests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CryptoConverter.Tests.Functional;

/// <summary>
/// Tests funcionales de la API.
/// Levantan la aplicación completa (incluyendo CryptoService real)
/// pero con el HttpClient de CoinGecko simulado mediante MockHttp.
/// Verifican el comportamiento extremo a extremo de cada endpoint.
/// </summary>
public class ConversionApiFunctionalTests : IClassFixture<FunctionalWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ConversionApiFunctionalTests(FunctionalWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ═══════════════════════════════════════════════════════════
    // GET /api/conversion/precios — todos los precios
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPrecios_DevuelveCincoMonedasConPreciosReales()
    {
        // Act
        var response = await _client.GetAsync("/api/conversion/precios");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var precios = await response.Content.ReadFromJsonAsync<PreciosResponse>();
        precios.Should().NotBeNull();
        precios!.Precios.Should().HaveCount(5);
        precios.Precios.Should().AllSatisfy(p =>
        {
            p.PrecioEur.Should().BePositive();
            p.Simbolo.Should().NotBeNullOrEmpty();
            p.Nombre.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetPrecios_TodosLosPreciosCorrectos()
    {
        // Act
        var response = await _client.GetAsync("/api/conversion/precios");
        var precios  = await response.Content.ReadFromJsonAsync<PreciosResponse>();

        // Assert — verificar precios concretos de los mocks
        precios!.Precios.Should().ContainSingle(p => p.Simbolo == "BTC")
            .Which.PrecioEur.Should().Be(58000.50m);

        precios.Precios.Should().ContainSingle(p => p.Simbolo == "ETH")
            .Which.PrecioEur.Should().Be(3100.75m);

        precios.Precios.Should().ContainSingle(p => p.Simbolo == "ADA")
            .Which.PrecioEur.Should().Be(0.45m);
    }

    // ═══════════════════════════════════════════════════════════
    // GET /api/conversion/precios/{moneda}
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData("BTC", 58000.50,  2.35)]
    [InlineData("ETH", 3100.75,  -1.20)]
    [InlineData("BNB", 520.30,    0.85)]
    [InlineData("SOL", 145.60,    4.10)]
    [InlineData("ADA", 0.45,     -0.30)]
    public async Task GetPrecioMoneda_CadaMonedaDevuelvePrecioYVariacionCorrectos(
        string moneda, double precioEsperado, double variacionEsperada)
    {
        // Act
        var response = await _client.GetAsync($"/api/conversion/precios/{moneda}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var precio = await response.Content.ReadFromJsonAsync<PrecioCrypto>();
        precio.Should().NotBeNull();
        precio!.PrecioEur.Should().Be((decimal)precioEsperado);
        precio.Variacion24hPct.Should().Be((decimal)variacionEsperada);
    }

    // ═══════════════════════════════════════════════════════════
    // POST /api/conversion/convertir
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task PostConvertir_1000EurosABitcoin_CalculaCorrectoConPrecioMock()
    {
        // Arrange — Bitcoin mockeado a 58000.50€
        // 1000 / 58000.50 ≈ 0.01724...
        var request = new ConversionRequest { Euros = 1000m, Moneda = CryptoMoneda.BTC };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversion/convertir", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<ConversionResponse>();
        resultado.Should().NotBeNull();
        resultado!.Euros.Should().Be(1000m);
        resultado.Moneda.Should().Be("BTC");
        resultado.PrecioUnitarioEur.Should().Be(58000.50m);
        resultado.CantidadCrypto.Should().BeApproximately(0.01724m, precision: 0.0001m);
        resultado.FechaConsulta.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task PostConvertir_1000EurosAEthereum_CalculaCorrectoConPrecioMock()
    {
        // Arrange — Ethereum mockeado a 3100.75€
        // 1000 / 3100.75 ≈ 0.32249...
        var request = new ConversionRequest { Euros = 1000m, Moneda = CryptoMoneda.ETH };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversion/convertir", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<ConversionResponse>();
        resultado!.CantidadCrypto.Should().BeApproximately(0.32249m, precision: 0.0001m);
        resultado.Variacion24hPct.Should().Be(-1.20m);
    }

    [Theory]
    [InlineData(100,    "BTC")]
    [InlineData(500,    "ETH")]
    [InlineData(50,     "ADA")]
    [InlineData(10000,  "BNB")]
    [InlineData(0.01,   "SOL")]
    public async Task PostConvertir_VariasCantidadesYMonedas_SiempreDevuelve200(
        double euros, string moneda)
    {
        // Arrange
        var monedaEnum = Enum.Parse<CryptoMoneda>(moneda);
        var request    = new ConversionRequest
        {
            Euros  = (decimal)euros,
            Moneda = monedaEnum,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversion/convertir", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<ConversionResponse>();
        resultado!.CantidadCrypto.Should().BePositive();
        resultado.Euros.Should().Be((decimal)euros);
    }

    // ═══════════════════════════════════════════════════════════
    // Validaciones de entrada — errores 400
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(-0.001)]
    public async Task PostConvertir_EurosInvalidos_Devuelve400(decimal euros)
    {
        // Arrange
        var request = new ConversionRequest { Euros = euros, Moneda = CryptoMoneda.BTC };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversion/convertir", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Codigo.Should().Be("CANTIDAD_INVALIDA");
        error.Mensaje.Should().NotBeNullOrEmpty();
    }

    // ═══════════════════════════════════════════════════════════
    // GET /api/conversion/convertir?euros=&moneda= (query string)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetConvertirQuery_1000EurosABTC_DevuelveResultadoCorrecto()
    {
        // Act
        var response = await _client.GetAsync("/api/conversion/convertir?euros=1000&moneda=BTC");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<ConversionResponse>();
        resultado!.Moneda.Should().Be("BTC");
        resultado.Euros.Should().Be(1000m);
        resultado.CantidadCrypto.Should().BePositive();
    }

    // ═══════════════════════════════════════════════════════════
    // Estructura de la respuesta
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task PostConvertir_RespuestaContieneTodasLasPropiedades()
    {
        // Arrange
        var request = new ConversionRequest { Euros = 250m, Moneda = CryptoMoneda.ETH };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversion/convertir", request);
        var resultado = await response.Content.ReadFromJsonAsync<ConversionResponse>();

        // Assert — verificar que todas las propiedades tienen valores coherentes
        resultado.Should().NotBeNull();
        resultado!.Euros.Should().Be(250m);
        resultado.Moneda.Should().Be("ETH");
        resultado.CantidadCrypto.Should().BePositive();
        resultado.PrecioUnitarioEur.Should().BePositive();
        resultado.FechaConsulta.Should().NotBe(default);
        // La variación puede ser negativa, solo verificamos que no es nulo
        resultado.Variacion24hPct.Should().NotBe(decimal.MinValue);
    }

    [Fact]
    public async Task GetPrecioMoneda_RespuestaContieneTodasLasPropiedades()
    {
        // Act
        var response = await _client.GetAsync("/api/conversion/precios/BTC");
        var precio   = await response.Content.ReadFromJsonAsync<PrecioCrypto>();

        // Assert
        precio.Should().NotBeNull();
        precio!.Simbolo.Should().Be("BTC");
        precio.Nombre.Should().Be("Bitcoin");
        precio.PrecioEur.Should().BePositive();
        precio.CapitalizacionEur.Should().BePositive();
        precio.UltimaActualizacion.Should().NotBe(default);
    }
}
