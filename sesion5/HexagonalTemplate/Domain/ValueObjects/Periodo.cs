namespace GestionAcademica.Domain.ValueObjects;

/// <summary>
/// Periodo académico como Value Object inmutable.
/// Encapsula la validación del formato "YYYY-SX".
/// </summary>
public sealed class Periodo
{
    public int    Anyo     { get; }
    public int    Semestre { get; }
    public string Codigo   => $"{Anyo}-S{Semestre}";

    private Periodo(int anyo, int semestre)
    {
        Anyo     = anyo;
        Semestre = semestre;
    }

    public static Periodo Crear(int anyo, int semestre)
    {
        if (anyo < 2000 || anyo > 2100)
            throw new ArgumentOutOfRangeException(nameof(anyo),
                "El año debe estar entre 2000 y 2100.");

        if (semestre is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(semestre),
                "El semestre debe ser 1 o 2.");

        return new Periodo(anyo, semestre);
    }

    public static Periodo Actual()
    {
        var hoy = DateTime.UtcNow;
        return new Periodo(hoy.Year, hoy.Month <= 6 ? 1 : 2);
    }

    public override bool Equals(object? obj)
        => obj is Periodo otro && Anyo == otro.Anyo && Semestre == otro.Semestre;

    public override int GetHashCode()
        => HashCode.Combine(Anyo, Semestre);

    public override string ToString() => Codigo;
}