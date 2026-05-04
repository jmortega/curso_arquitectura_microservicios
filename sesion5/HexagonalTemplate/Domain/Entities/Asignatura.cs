namespace GestionAcademica.Domain.Entities;

public class Asignatura
{
    public int    Id       { get; private set; }
    public string Codigo   { get; private set; } = string.Empty;
    public string Nombre   { get; private set; } = string.Empty;
    public int    Creditos { get; private set; }
    public bool   Activa   { get; private set; }

    private Asignatura() { }

    public static Asignatura Crear(int id, string codigo, string nombre, int creditos)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código es obligatorio.");

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        if (creditos is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(creditos),
                "Los créditos deben estar entre 1 y 12.");

        return new Asignatura
        {
            Id       = id,
            Codigo   = codigo.Trim().ToUpperInvariant(),
            Nombre   = nombre.Trim(),
            Creditos = creditos,
            Activa   = true
        };
    }

    /// <summary>
    /// Regla de negocio: una asignatura desactivada no puede volver a activarse
    /// sin pasar por un proceso administrativo explícito.
    /// </summary>
    public void Desactivar()
    {
        if (!Activa)
            throw new InvalidOperationException(
                $"La asignatura '{Nombre}' ya está inactiva.");
        Activa = false;
    }
}