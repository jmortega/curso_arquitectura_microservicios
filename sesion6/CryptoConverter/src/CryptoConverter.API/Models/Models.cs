namespace CryptoConverter.API.Models;

// ──────────────────────────────────────────────────────────────
// Criptomonedas soportadas
// ──────────────────────────────────────────────────────────────

/// <summary>Criptomonedas disponibles para la conversión.</summary>
public enum CryptoMoneda
{
    BTC,        // Bitcoin
    ETH,        // Ethereum
    BNB,        // Binance Coin
    SOL,        // Solana
    ADA         // Cardano
}

// ──────────────────────────────────────────────────────────────
// Request
// ──────────────────────────────────────────────────────────────

/// <summary>Petición de conversión de euros a criptomoneda.</summary>
public class ConversionRequest
{
    /// <summary>Cantidad en euros a convertir. Debe ser mayor que 0.</summary>
    /// <example>1000</example>
    public decimal Euros { get; set; }

    /// <summary>Criptomoneda destino de la conversión.</summary>
    /// <example>BTC</example>
    public CryptoMoneda Moneda { get; set; }
}

// ──────────────────────────────────────────────────────────────
// Response
// ──────────────────────────────────────────────────────────────

/// <summary>Resultado de una conversión de euros a criptomoneda.</summary>
public class ConversionResponse
{
    /// <summary>Cantidad original en euros.</summary>
    public decimal Euros { get; set; }

    /// <summary>Criptomoneda resultado.</summary>
    public string Moneda { get; set; } = string.Empty;

    /// <summary>Cantidad equivalente en la criptomoneda.</summary>
    public decimal CantidadCrypto { get; set; }

    /// <summary>Precio unitario de 1 unidad de la cripto en euros.</summary>
    public decimal PrecioUnitarioEur { get; set; }

    /// <summary>Fecha y hora UTC de la consulta del precio.</summary>
    public DateTime FechaConsulta { get; set; }

    /// <summary>Variación del precio en las últimas 24h (%).</summary>
    public decimal Variacion24hPct { get; set; }
}

/// <summary>Precio de mercado de una criptomoneda.</summary>
public class PrecioCrypto
{
    public string Simbolo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Precio actual en euros.</summary>
    public decimal PrecioEur { get; set; }

    /// <summary>Variación de precio en las últimas 24 horas (%).</summary>
    public decimal Variacion24hPct { get; set; }

    /// <summary>Capitalización de mercado en euros.</summary>
    public decimal CapitalizacionEur { get; set; }

    public DateTime UltimaActualizacion { get; set; }
}

/// <summary>Respuesta que contiene los precios de todas las criptomonedas.</summary>
public class PreciosResponse
{
    public IReadOnlyList<PrecioCrypto> Precios { get; set; } = [];
    public DateTime FechaConsulta { get; set; } = DateTime.UtcNow;
}

/// <summary>Respuesta de error estándar de la API.</summary>
public class ErrorResponse
{
    public string Codigo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
