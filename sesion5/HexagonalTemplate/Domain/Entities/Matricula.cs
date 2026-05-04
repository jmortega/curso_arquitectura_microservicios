namespace GestionAcademica.Domain.Entities;

public class Matricula
{
    public int      Id          { get; private set; }
    public int      AlumnoId    { get; private set; }
    public int      AsignaturaId { get; private set; }
    public string   Periodo     { get; private set; } = string.Empty;
    public DateTime FechaAlta   { get; private set; }
    public bool     Activa      { get; private set; }

    // Navegación (EF Core)
    public Alumno?    Alumno    { get; private set; }
    public Asignatura? Asignatura { get; private set; }

    private Matricula() { }

    public static Matricula Crear(int alumnoId, int asignaturaId, string periodo)
    {
        if (string.IsNullOrWhiteSpace(periodo))
            throw new ArgumentException("El periodo es obligatorio.");

        return new Matricula
        {
            AlumnoId     = alumnoId,
            AsignaturaId = asignaturaId,
            Periodo      = periodo.Trim(),
            FechaAlta    = DateTime.UtcNow,
            Activa       = true
        };
    }

    public void Cancelar()
    {
        if (!Activa)
            throw new InvalidOperationException("La matrícula ya está cancelada.");
        Activa = false;
    }
}