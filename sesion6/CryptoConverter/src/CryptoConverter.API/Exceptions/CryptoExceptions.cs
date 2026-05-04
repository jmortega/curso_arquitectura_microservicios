namespace CryptoConverter.API.Exceptions;

/// <summary>Se lanza cuando la criptomoneda solicitada no está soportada.</summary>
public class MonedaNoSoportadaException : Exception
{
    public string Moneda { get; }

    public MonedaNoSoportadaException(string moneda)
        : base($"La criptomoneda '{moneda}' no está soportada.")
    {
        Moneda = moneda;
    }
}

/// <summary>Se lanza cuando no se puede obtener el precio del proveedor externo.</summary>
public class PrecioNoDisponibleException : Exception
{
    public string Moneda { get; }

    public PrecioNoDisponibleException(string moneda, Exception? inner = null)
        : base($"No se pudo obtener el precio de '{moneda}'. Inténtalo de nuevo.", inner)
    {
        Moneda = moneda;
    }
}

/// <summary>Se lanza cuando la cantidad en euros no es válida.</summary>
public class CantidadInvalidaException : Exception
{
    public CantidadInvalidaException(decimal cantidad)
        : base($"La cantidad '{cantidad}' no es válida. Debe ser mayor que 0.") { }
}
