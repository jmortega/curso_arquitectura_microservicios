using CryptoConverter.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;

namespace CryptoConverter.Tests.Helpers;

// ──────────────────────────────────────────────────────────────
// Factory para tests de INTEGRACIÓN
// Reemplaza ICryptoService por un Mock de Moq.
// El controlador y el pipeline HTTP son reales.
// ──────────────────────────────────────────────────────────────

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<ICryptoService> CryptoServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICryptoService>();
            services.AddSingleton(CryptoServiceMock.Object);
        });
    }
}

// ──────────────────────────────────────────────────────────────
// Factory para tests FUNCIONALES
//
// Estrategia: inyectar CryptoService directamente construido con
// mockHttp.ToHttpClient() — sin pasar por la factoría de
// HttpClients ni por Polly, que interferían con MockHttp.
//
// Se usa Fallback en lugar de When(url) para evitar el problema
// de que MockHttp URL-encodifica el '*' en query strings
// (?ids=bitcoin* → ids=bitcoin%2A → sin match).
// ──────────────────────────────────────────────────────────────

public class FunctionalWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly MockHttpMessageHandler _mockHttp = new();

    public FunctionalWebApplicationFactory()
    {
        // Fallback: responde a CUALQUIER petición HTTP con TodosLosPrecios.
        // CryptoService extrae del JSON la moneda que necesita, así que
        // devolver siempre el JSON completo funciona para todos los casos.
        _mockHttp.Fallback.Respond(
            "application/json",
            CoinGeckoResponses.TodosLosPrecios);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Eliminar el ICryptoService registrado por Program.cs
            services.RemoveAll<ICryptoService>();

            // Registrar un CryptoService nuevo construido con el HttpClient
            // del mock directamente — sin Polly, sin IHttpClientFactory.
            services.AddSingleton<ICryptoService>(sp =>
            {
                var httpClient = _mockHttp.ToHttpClient();
                httpClient.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
                httpClient.DefaultRequestHeaders.UserAgent
                    .ParseAdd("CryptoConverter/1.0 (test)");

                var logger = sp.GetRequiredService<ILogger<CryptoService>>();
                return new CryptoService(httpClient, logger);
            });
        });
    }
}

// ──────────────────────────────────────────────────────────────
// JSON de respuestas simuladas — formato /simple/price de CoinGecko
// ──────────────────────────────────────────────────────────────

public static class CoinGeckoResponses
{
    /// <summary>
    /// JSON con todas las monedas. Se usa como Fallback en tests funcionales
    /// y como respuesta individual en tests unitarios.
    /// CryptoService filtra la moneda concreta por clave del diccionario.
    /// </summary>
    public const string TodosLosPrecios = """
        {
          "bitcoin":     { "eur": 58000.50, "eur_24h_change":  2.35, "eur_market_cap": 1140000000000 },
          "ethereum":    { "eur":  3100.75, "eur_24h_change": -1.20, "eur_market_cap":  373000000000 },
          "binancecoin": { "eur":   520.30, "eur_24h_change":  0.85, "eur_market_cap":   80000000000 },
          "solana":      { "eur":   145.60, "eur_24h_change":  4.10, "eur_market_cap":   67000000000 },
          "cardano":     { "eur":     0.45, "eur_24h_change": -0.30, "eur_market_cap":   16000000000 }
        }
        """;

    // Respuestas individuales usadas en tests unitarios
    public const string SoloBitcoin = """
        { "bitcoin": { "eur": 58000.50, "eur_24h_change": 2.35, "eur_market_cap": 1140000000000 } }
        """;

    public const string SoloEthereum = """
        { "ethereum": { "eur": 3100.75, "eur_24h_change": -1.20, "eur_market_cap": 373000000000 } }
        """;

    public const string SoloBnb = """
        { "binancecoin": { "eur": 520.30, "eur_24h_change": 0.85, "eur_market_cap": 80000000000 } }
        """;

    public const string SoloSolana = """
        { "solana": { "eur": 145.60, "eur_24h_change": 4.10, "eur_market_cap": 67000000000 } }
        """;

    public const string SoloCardano = """
        { "cardano": { "eur": 0.45, "eur_24h_change": -0.30, "eur_market_cap": 16000000000 } }
        """;
}