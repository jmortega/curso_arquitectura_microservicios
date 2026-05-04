namespace Calculadora.Core.Exceptions;

/// <summary>Se lanza cuando se intenta dividir entre cero.</summary>
public class DivisionPorCeroException : Exception
{
    public DivisionPorCeroException()
        : base("No se puede dividir entre cero.") { }

    public DivisionPorCeroException(string mensaje)
        : base(mensaje) { }
}

/// <summary>Se lanza cuando el resultado desborda el límite permitido.</summary>
public class DesbordamientoException : Exception
{
    public DesbordamientoException(string mensaje) : base(mensaje) { }
}

/// <summary>Se lanza cuando un argumento tiene un valor inválido.</summary>
public class ArgumentoInvalidoException : Exception
{
    public string NombreParametro { get; }

    public ArgumentoInvalidoException(string nombreParametro, string mensaje)
        : base(mensaje)
    {
        NombreParametro = nombreParametro;
    }
}
