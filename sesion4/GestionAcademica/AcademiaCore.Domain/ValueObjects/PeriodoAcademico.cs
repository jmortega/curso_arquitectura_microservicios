// ── Domain/Matriculacion/ValueObjects/PeriodoAcademico.cs ────────────
namespace AcademiaCore.Domain.Matriculacion.ValueObjects;

using AcademiaCore.Domain.Common;

/// <summary>
/// Periodo académico: año y semestre (ej. 2024-S1, 2024-S2).
/// Encapsula las reglas de cuándo un periodo está activo o ha cerrado.
/// </summary>
public sealed class PeriodoAcademico : ValueObject
{
    public int  Anyo     { get; }
    public int  Semestre { get; }
    public string Codigo  => $"{Anyo}-S{Semestre}";

    // Constructor sin parámetros requerido por EF Core
    private PeriodoAcademico() { }
    
    private PeriodoAcademico(int anyo, int semestre)
    {
        Anyo     = anyo;
        Semestre = semestre;
    }

    public static PeriodoAcademico Crear(int anyo, int semestre)
    {
        if (anyo < 2000 || anyo > 2100)
            throw new ArgumentOutOfRangeException(nameof(anyo),
                "El año académico debe estar entre 2000 y 2100.");

        if (semestre is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(semestre),
                "El semestre debe ser 1 o 2.");

        return new PeriodoAcademico(anyo, semestre);
    }


    public static PeriodoAcademico Actual()
    {
        var hoy = DateTime.UtcNow;
        int semestre = hoy.Month <= 6 ? 1 : 2;
        return new PeriodoAcademico(hoy.Year, semestre);
    }

    public bool EsAnteriorA(PeriodoAcademico otro)
        => Anyo < otro.Anyo || (Anyo == otro.Anyo && Semestre < otro.Semestre);

    public override string ToString() => Codigo;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Anyo;
        yield return Semestre;
    }
}