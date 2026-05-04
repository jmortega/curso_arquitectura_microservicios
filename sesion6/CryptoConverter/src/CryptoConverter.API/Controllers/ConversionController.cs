using CryptoConverter.API.Exceptions;
using CryptoConverter.API.Models;
using CryptoConverter.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoConverter.API.Controllers;

/// <summary>
/// Endpoints para convertir euros a criptomonedas y consultar precios de mercado.
/// Los precios se obtienen en tiempo real desde CoinGecko.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConversionController : ControllerBase
{
    private readonly ICryptoService _cryptoService;
    private readonly ILogger<ConversionController> _logger;

    public ConversionController(ICryptoService cryptoService,
                                ILogger<ConversionController> logger)
    {
        _cryptoService = cryptoService;
        _logger        = logger;
    }

    // ──────────────────────────────────────────────────────────
    // GET /api/conversion/precios
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene los precios actuales de todas las criptomonedas soportadas en euros.
    /// </summary>
    /// <returns>Lista de precios con variación de 24h y capitalización.</returns>
    /// <response code="200">Precios obtenidos correctamente.</response>
    /// <response code="503">No se pudo conectar con el proveedor de precios.</response>
    [HttpGet("precios")]
    [ProducesResponseType(typeof(PreciosResponse),  StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse),    StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ObtenerPrecios()
    {
        try
        {
            var precios = await _cryptoService.ObtenerTodosLosPreciosAsync();
            return Ok(precios);
        }
        catch (PrecioNoDisponibleException ex)
        {
            _logger.LogWarning(ex, "Precios no disponibles");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse
                {
                    Codigo  = "PRECIO_NO_DISPONIBLE",
                    Mensaje = ex.Message,
                });
        }
    }

    // ──────────────────────────────────────────────────────────
    // GET /api/conversion/precios/{moneda}
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene el precio actual de una criptomoneda específica en euros.
    /// </summary>
    /// <param name="moneda">Símbolo de la criptomoneda (BTC, ETH, BNB, SOL, ADA).</param>
    /// <returns>Precio actual con variación de 24h.</returns>
    /// <response code="200">Precio obtenido correctamente.</response>
    /// <response code="400">La criptomoneda no está soportada.</response>
    /// <response code="503">No se pudo conectar con el proveedor de precios.</response>
    [HttpGet("precios/{moneda}")]
    [ProducesResponseType(typeof(PrecioCrypto),  StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ObtenerPrecio([FromRoute] CryptoMoneda moneda)
    {
        try
        {
            var precio = await _cryptoService.ObtenerPrecioAsync(moneda);
            return Ok(precio);
        }
        catch (MonedaNoSoportadaException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Codigo  = "MONEDA_NO_SOPORTADA",
                Mensaje = ex.Message,
            });
        }
        catch (PrecioNoDisponibleException ex)
        {
            _logger.LogWarning(ex, "Precio no disponible para {Moneda}", moneda);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse
                {
                    Codigo  = "PRECIO_NO_DISPONIBLE",
                    Mensaje = ex.Message,
                });
        }
    }

    // ──────────────────────────────────────────────────────────
    // POST /api/conversion/convertir
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Convierte una cantidad en euros a la criptomoneda indicada.
    /// </summary>
    /// <param name="request">Cantidad en euros y moneda destino.</param>
    /// <returns>Resultado de la conversión con el precio aplicado.</returns>
    /// <response code="200">Conversión realizada correctamente.</response>
    /// <response code="400">Cantidad inválida o moneda no soportada.</response>
    /// <response code="503">No se pudo obtener el precio en este momento.</response>
    [HttpPost("convertir")]
    [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse),      StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse),      StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Convertir([FromBody] ConversionRequest request)
    {
        try
        {
            var resultado = await _cryptoService.ConvertirAsync(request.Euros, request.Moneda);
            _logger.LogInformation(
                "Conversión realizada: {Euros}€ → {Cantidad} {Moneda}",
                request.Euros, resultado.CantidadCrypto, request.Moneda);
            return Ok(resultado);
        }
        catch (CantidadInvalidaException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Codigo  = "CANTIDAD_INVALIDA",
                Mensaje = ex.Message,
            });
        }
        catch (MonedaNoSoportadaException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Codigo  = "MONEDA_NO_SOPORTADA",
                Mensaje = ex.Message,
            });
        }
        catch (PrecioNoDisponibleException ex)
        {
            _logger.LogError(ex, "Error al convertir {Euros}€ a {Moneda}",
                request.Euros, request.Moneda);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ErrorResponse
                {
                    Codigo  = "PRECIO_NO_DISPONIBLE",
                    Mensaje = ex.Message,
                });
        }
    }

    // ──────────────────────────────────────────────────────────
    // GET /api/conversion/convertir?euros=1000&moneda=BTC
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Conversión rápida de euros a criptomoneda mediante parámetros de query.
    /// </summary>
    /// <param name="euros">Cantidad en euros a convertir.</param>
    /// <param name="moneda">Criptomoneda destino (BTC, ETH, BNB, SOL, ADA).</param>
    /// <returns>Resultado de la conversión.</returns>
    /// <response code="200">Conversión realizada correctamente.</response>
    /// <response code="400">Parámetros inválidos.</response>
    [HttpGet("convertir")]
    [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse),      StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse),      StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ConvertirQuery(
        [FromQuery] decimal euros,
        [FromQuery] CryptoMoneda moneda)
    {
        return await Convertir(new ConversionRequest { Euros = euros, Moneda = moneda });
    }
}
