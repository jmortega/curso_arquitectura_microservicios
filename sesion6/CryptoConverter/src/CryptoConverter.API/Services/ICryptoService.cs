using CryptoConverter.API.Models;

namespace CryptoConverter.API.Services;

/// <summary>
/// Contrato del servicio de conversión de euros a criptomonedas.
/// </summary>
public interface ICryptoService
{
    /// <summary>
    /// Convierte una cantidad en euros a la criptomoneda indicada.
    /// </summary>
    /// <param name="euros">Cantidad en euros. Debe ser mayor que 0.</param>
    /// <param name="moneda">Criptomoneda destino.</param>
    Task<ConversionResponse> ConvertirAsync(decimal euros, CryptoMoneda moneda);

    /// <summary>
    /// Obtiene el precio actual de una criptomoneda en euros.
    /// </summary>
    Task<PrecioCrypto> ObtenerPrecioAsync(CryptoMoneda moneda);

    /// <summary>
    /// Obtiene los precios de todas las criptomonedas soportadas.
    /// </summary>
    Task<PreciosResponse> ObtenerTodosLosPreciosAsync();
}
