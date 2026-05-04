using System.Text.Json;
using System.Text.Json.Serialization;
using CryptoConverter.API.Exceptions;
using CryptoConverter.API.Models;

namespace CryptoConverter.API.Services;

/// <summary>
/// Implementación del servicio de conversión que obtiene precios
/// desde la API pública de CoinGecko usando el endpoint /simple/price.
///
/// Se usa /simple/price en lugar de /coins/{id} porque:
///  - Una sola llamada HTTP para todos los precios (evita rate limiting)
///  - Respuesta mucho más ligera (solo los datos necesarios)
///  - Menos probabilidad de superar el límite gratuito de CoinGecko
/// </summary>
public class CryptoService : ICryptoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CryptoService> _logger;

    // Mapa: enum → ID de CoinGecko
    private static readonly Dictionary<CryptoMoneda, string> _coinGeckoIds = new()
    {
        { CryptoMoneda.BTC, "bitcoin"      },
        { CryptoMoneda.ETH, "ethereum"     },
        { CryptoMoneda.BNB, "binancecoin"  },
        { CryptoMoneda.SOL, "solana"       },
        { CryptoMoneda.ADA, "cardano"      },
    };

    // Mapa inverso: ID de CoinGecko → enum
    private static readonly Dictionary<string, CryptoMoneda> _idAEnum =
        _coinGeckoIds.ToDictionary(kv => kv.Value, kv => kv.Key);

    // Mapa: enum → nombre legible
    private static readonly Dictionary<CryptoMoneda, string> _nombres = new()
    {
        { CryptoMoneda.BTC, "Bitcoin"      },
        { CryptoMoneda.ETH, "Ethereum"     },
        { CryptoMoneda.BNB, "Binance Coin" },
        { CryptoMoneda.SOL, "Solana"       },
        { CryptoMoneda.ADA, "Cardano"      },
    };

    // IDs de CoinGecko separados por coma para la query
    private static readonly string _todosLosIds =
        string.Join(",", _coinGeckoIds.Values);

    public CryptoService(HttpClient httpClient, ILogger<CryptoService> logger)
    {
        _httpClient = httpClient;
        _logger     = logger;
    }

    // ──────────────────────────────────────────────────────────
    // Conversión
    // ──────────────────────────────────────────────────────────

    public async Task<ConversionResponse> ConvertirAsync(decimal euros, CryptoMoneda moneda)
    {
        if (euros <= 0)
            throw new CantidadInvalidaException(euros);

        var precio = await ObtenerPrecioAsync(moneda);

        if (precio.PrecioEur <= 0)
            throw new PrecioNoDisponibleException(moneda.ToString());

        var cantidad = euros / precio.PrecioEur;

        return new ConversionResponse
        {
            Euros             = euros,
            Moneda            = moneda.ToString(),
            CantidadCrypto    = Math.Round(cantidad, 8),
            PrecioUnitarioEur = precio.PrecioEur,
            FechaConsulta     = DateTime.UtcNow,
            Variacion24hPct   = precio.Variacion24hPct,
        };
    }

    // ──────────────────────────────────────────────────────────
    // Precio de UNA criptomoneda
    // Usa /simple/price con un único ID para minimizar la carga
    // ──────────────────────────────────────────────────────────

    public async Task<PrecioCrypto> ObtenerPrecioAsync(CryptoMoneda moneda)
    {
        if (!_coinGeckoIds.TryGetValue(moneda, out var geckoId))
            throw new MonedaNoSoportadaException(moneda.ToString());

        _logger.LogInformation("Obteniendo precio de {Moneda}", moneda);

        // Una sola llamada ligera para la moneda solicitada
        var url = BuildSimplePriceUrl(geckoId);

        try
        {
            var response = await _httpClient.GetAsync(url);

            // Loguear el status code para facilitar diagnóstico
            _logger.LogInformation(
                "CoinGecko respondió {StatusCode} para {Moneda}",
                (int)response.StatusCode, moneda);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "CoinGecko devolvió {StatusCode}. Body: {Body}",
                    (int)response.StatusCode, body);
                throw new PrecioNoDisponibleException(moneda.ToString());
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Respuesta CoinGecko: {Json}", json);

            var data = JsonSerializer.Deserialize<Dictionary<string, SimplePriceDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data is null || !data.TryGetValue(geckoId, out var precioDto))
                throw new PrecioNoDisponibleException(moneda.ToString());

            return new PrecioCrypto
            {
                Simbolo             = moneda.ToString(),
                Nombre              = _nombres[moneda],
                PrecioEur           = precioDto.Eur,
                Variacion24hPct     = precioDto.EurChange24H,
                CapitalizacionEur   = precioDto.EurMarketCap,
                UltimaActualizacion = DateTime.UtcNow,
            };
        }
        catch (PrecioNoDisponibleException)
        {
            throw;  // relanzar sin envolver
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado obteniendo precio de {Moneda}", moneda);
            throw new PrecioNoDisponibleException(moneda.ToString(), ex);
        }
    }

    // ──────────────────────────────────────────────────────────
    // Precios de TODAS las monedas — UNA sola llamada HTTP
    // ──────────────────────────────────────────────────────────

    public async Task<PreciosResponse> ObtenerTodosLosPreciosAsync()
    {
        _logger.LogInformation("Obteniendo precios de todas las monedas en una sola llamada");

        // Una sola petición con todos los IDs
        var url = BuildSimplePriceUrl(_todosLosIds);

        try
        {
            var response = await _httpClient.GetAsync(url);

            _logger.LogInformation(
                "CoinGecko respondió {StatusCode} para todos los precios",
                (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "CoinGecko devolvió {StatusCode}. Body: {Body}",
                    (int)response.StatusCode, body);
                throw new PrecioNoDisponibleException("todos");
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Respuesta CoinGecko todos: {Json}", json);

            var data = JsonSerializer.Deserialize<Dictionary<string, SimplePriceDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data is null || data.Count == 0)
                throw new PrecioNoDisponibleException("todos");

            var precios = data
                .Where(kv => _idAEnum.ContainsKey(kv.Key))
                .Select(kv =>
                {
                    var moneda = _idAEnum[kv.Key];
                    return new PrecioCrypto
                    {
                        Simbolo             = moneda.ToString(),
                        Nombre              = _nombres[moneda],
                        PrecioEur           = kv.Value.Eur,
                        Variacion24hPct     = kv.Value.EurChange24H,
                        CapitalizacionEur   = kv.Value.EurMarketCap,
                        UltimaActualizacion = DateTime.UtcNow,
                    };
                })
                .ToList();

            return new PreciosResponse
            {
                Precios       = precios,
                FechaConsulta = DateTime.UtcNow,
            };
        }
        catch (PrecioNoDisponibleException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado obteniendo todos los precios");
            throw new PrecioNoDisponibleException("todos", ex);
        }
    }

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Construye la URL del endpoint /simple/price de CoinGecko.
    /// Un solo endpoint que devuelve precio, variación 24h y market cap en euros.
    /// </summary>
    private static string BuildSimplePriceUrl(string ids) =>
        $"simple/price?ids={ids}" +
        $"&vs_currencies=eur" +
        $"&include_24hr_change=true" +
        $"&include_market_cap=true";

    // ──────────────────────────────────────────────────────────
    // DTO interno para /simple/price
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Mapea la respuesta JSON de /simple/price:
    /// {
    ///   "bitcoin": { "eur": 58000, "eur_24h_change": 2.35, "eur_market_cap": 1140000000000 }
    /// }
    /// </summary>
    private sealed class SimplePriceDto
    {
        [JsonPropertyName("eur")]
        public decimal Eur { get; set; }

        [JsonPropertyName("eur_24h_change")]
        public decimal EurChange24H { get; set; }

        [JsonPropertyName("eur_market_cap")]
        public decimal EurMarketCap { get; set; }
    }
}
