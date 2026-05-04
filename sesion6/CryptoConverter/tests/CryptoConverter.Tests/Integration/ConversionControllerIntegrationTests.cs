using CryptoConverter.API.Exceptions;
using CryptoConverter.API.Models;
using CryptoConverter.API.Services;
using CryptoConverter.Tests.Helpers;
using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CryptoConverter.Tests.Integration;

/// <summary>
/// Tests de integración del ConversionController.
/// Levantan la aplicación real con WebApplicationFactory pero
/// sustituyen ICryptoService por un Mock para aislar la capa HTTP.
/// </summary>
public class ConversionControllerIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Mock<ICryptoService> _mockService;

    public ConversionControllerIntegrationTests(TestWebApplicationFactory factory)
    {
        _client      = factory.CreateClient();
        _mockService = factory.CryptoServiceMock;
        _mockService.Reset();     // limpia configuraciones entre tests
    }

    // ─── Datos de prueba reutilizables ─────────────────────────
    private static PrecioCrypto PrecioBtcFake => new()
    {
        Simbolo             = "BTC",
        Nombre              = "Bitcoin",
        PrecioEur           = 58000m,
        Variacion24hPct     = 2.5m,
        CapitalizacionEur   = 1_140_000_000_000m,
        UltimaActualizacion = DateTime.UtcNow,
    };

    private static ConversionResponse ConversionBtcFake => new()
    {
        Euros             = 1000m,
        Moneda            = "BTC",
        CantidadCrypto    = 0.01724m,
        PrecioUnitarioEur = 58000m,
        FechaConsulta     = DateTime.UtcNow,
        Variacion24hPct   = 2.5m,
    };

    // ═══════════════════════════════════════════════════════════
    // GET /api/conversion/precios/{moneda}
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPrecioMoneda_BTC_Devuelve200ConPrecio()
    {
        // Arrange
        _mockService
            .Setup(s => s.ObtenerPrecioAsync(CryptoMoneda.BTC))
            .ReturnsAsync(PrecioBtcFake);

        // Act
        var response = await _client.GetAsync("/api/conversion/precios/BTC");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var precio = await response.Content.ReadFromJsonAsync<PrecioCrypto>();
        precio.Should().NotBeNull();
        precio!.Simbolo.Should().Be("BTC");
        precio.PrecioEur.Should().Be(58000m);
    }

    [Fact]
    public async Task GetPrecioMoneda_CoinGeckoFalla_Devuelve503()
    {
        // Arrange
        _mockService
            .Setup(s => s.ObtenerPrecioAsync(CryptoMoneda.ETH))
            .ThrowsAsync(new PrecioNoDisponibleException("ETH"));

        // Act
        var response = await _client.GetAsync("/api/conversion/precios/ETH");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Codigo.Should().Be("PRECIO_NO_DISPONIBLE");
    }

    // ═══════════════════════════════════════════════════════════
    // GET /api/conversion/precios
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPrecios_Devuelve200ConListaDePrecios()
    {
        // Arrange
        var preciosEsperados = new PreciosResponse
        {
            Precios = new List<PrecioCrypto>
            {
                PrecioBtcFake,
                new() { Simbolo = "ETH", Nombre = "Ethereum", PrecioEur = 3100m },
            },
        };
        _mockService
            .Setup(s => s.ObtenerTodosLosPreciosAsync())
            .ReturnsAsync(preciosEsperados);

        // Act
        var response = await _client.GetAsync("/api/conversion/precios");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var precios = await response.Content.ReadFromJsonAsync<PreciosResponse>();
        precios!.Precios.Should().HaveCount(2);
    }

    // ═══════════════════════════════════════════════════════════
    // POST /api/conversion/convertir
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task PostConvertir_1000EurosABtc_Devuelve200ConResultado()
    {
        // Arrange
        _mockService
            .Setup(s => s.ConvertirAsync(1000m, CryptoMoneda.BTC))
            .ReturnsAsync(ConversionBtcFake);

        var request = new ConversionRequest { Euros = 1000m, Moneda = CryptoMoneda.BTC };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversion/convertir", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<ConversionResponse>();
        resultado!.Moneda.Should().Be("BTC");
        resultado.Euros.Should().Be(1000m);
        resultado.CantidadCrypto.Should().BePositive();
    }

    [Fact]
    public async Task PostConvertir_EurosCero_Devuelve400()
    {
        // Arrange
        _mockService
            .Setup(s => s.ConvertirAsync(0m, CryptoMoneda.BTC))
            .ThrowsAsync(new CantidadInvalidaException(0m));

        var request = new ConversionRequest { Euros = 0m, Moneda = CryptoMoneda.BTC };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversion/convertir", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Codigo.Should().Be("CANTIDAD_INVALIDA");
    }

    [Fact]
    public async Task PostConvertir_VerificaQueElServicioEsLlamadoExactamenteUnaVez()
    {
        // Arrange
        _mockService
            .Setup(s => s.ConvertirAsync(500m, CryptoMoneda.ETH))
            .ReturnsAsync(new ConversionResponse
            {
                Euros = 500m, Moneda = "ETH", CantidadCrypto = 0.16m
            });

        var request = new ConversionRequest { Euros = 500m, Moneda = CryptoMoneda.ETH };

        // Act
        await _client.PostAsJsonAsync("/api/conversion/convertir", request);

        // Assert — verificar la interacción con el servicio
        _mockService.Verify(
            s => s.ConvertirAsync(500m, CryptoMoneda.ETH),
            Times.Once);
    }

    // ═══════════════════════════════════════════════════════════
    // GET /api/conversion/convertir?euros=&moneda=
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetConvertirQuery_1000EurosETH_Devuelve200()
    {
        // Arrange
        _mockService
            .Setup(s => s.ConvertirAsync(1000m, CryptoMoneda.ETH))
            .ReturnsAsync(new ConversionResponse
            {
                Euros = 1000m, Moneda = "ETH", CantidadCrypto = 0.32m
            });

        // Act
        var response = await _client.GetAsync("/api/conversion/convertir?euros=1000&moneda=ETH");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<ConversionResponse>();
        resultado!.Moneda.Should().Be("ETH");
    }

    // ═══════════════════════════════════════════════════════════
    // Content-Type
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData("/api/conversion/precios")]
    [InlineData("/api/conversion/precios/BTC")]
    public async Task Endpoints_DevuelvenContentTypeJson(string url)
    {
        // Arrange
        _mockService
            .Setup(s => s.ObtenerTodosLosPreciosAsync())
            .ReturnsAsync(new PreciosResponse());
        _mockService
            .Setup(s => s.ObtenerPrecioAsync(It.IsAny<CryptoMoneda>()))
            .ReturnsAsync(PrecioBtcFake);

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/json");
    }
}
