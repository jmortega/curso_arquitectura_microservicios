// ── Domain/Entities/Asignatura.cs ────────────────────────────────────
namespace AcademiaCore.Domain.Entities;

using AcademiaCore.Domain.Common;

public class Asignatura : Entity<int>
{
    public string Nombre         { get; private set; } = string.Empty;
    public string Codigo         { get; private set; } = string.Empty;
    public int    Creditos       { get; private set; }
    public int    PlazasMaximas  { get; private set; }
    public int    PlazasOcupadas { get; private set; }
    public bool   Activa         { get; private set; }

    public int  PlazasDisponibles => PlazasMaximas - PlazasOcupadas;
    public bool TienePlazas       => PlazasDisponibles > 0;

    // Constructor sin parámetros requerido por EF Core
    private Asignatura() { }

    public static Asignatura Crear(int id, string codigo,
                                   string nombre, int creditos,
                                   int plazasMaximas)
    {
        if (creditos is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(creditos),
                "Los créditos deben estar entre 1 y 12.");

        if (plazasMaximas < 1)
            throw new ArgumentOutOfRangeException(nameof(plazasMaximas),
                "Debe haber al menos una plaza.");

        return new Asignatura
        {
            Id            = id,
            Codigo        = codigo.Trim().ToUpperInvariant(),
            Nombre        = nombre.Trim(),
            Creditos      = creditos,
            PlazasMaximas = plazasMaximas,
            Activa        = true
        };
    }

    internal void OcuparPlaza()
    {
        if (!TienePlazas)
            throw new InvalidOperationException(
                $"'{Nombre}' no tiene plazas disponibles.");
        PlazasOcupadas++;
    }

    internal void LiberarPlaza()
    {
        if (PlazasOcupadas == 0)
            throw new InvalidOperationException("No hay plazas ocupadas.");
        PlazasOcupadas--;
    }
}