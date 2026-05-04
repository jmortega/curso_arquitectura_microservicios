namespace Calculadora.Services;

/// <summary>Registra el historial de operaciones de la calculadora.</summary>
public class HistorialService
{
    private readonly List<EntradaHistorial> _entradas = new();

    public IReadOnlyList<EntradaHistorial> Entradas  => _entradas.AsReadOnly();
    public int  TotalOperaciones                     => _entradas.Count;
    public bool EstaVacio                            => !_entradas.Any();

    /// <summary>Registra una operación con su descripción y resultado.</summary>
    public void Registrar(string descripcion, double resultado)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ArgumentException(
                "La descripción no puede estar vacía.", nameof(descripcion));

        _entradas.Add(new EntradaHistorial(descripcion, resultado, DateTime.UtcNow));
    }

    /// <summary>Devuelve las últimas N entradas del historial.</summary>
    public IEnumerable<EntradaHistorial> ObtenerUltimas(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException(
                "La cantidad debe ser mayor que cero.", nameof(cantidad));

        return _entradas.TakeLast(cantidad);
    }

    /// <summary>Limpia todo el historial.</summary>
    public void Limpiar() => _entradas.Clear();
}

/// <summary>Entrada individual del historial.</summary>
public record EntradaHistorial(
    string   Descripcion,
    double   Resultado,
    DateTime FechaHora);
