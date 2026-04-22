using DapperAsignaturas;

public class Matriculacion
{
    public int        Id           { get; set; }
    public int        AlumnoId     { get; set; }
    public int        AsignaturaId { get; set; }
    public string     FechaAlta    { get; set; } = string.Empty;
    public string     Estado       { get; set; } = string.Empty; // Activa | Anulada | Superada
    public double?    Nota         { get; set; }                 // null hasta calificación
    public Asignatura Asignatura   { get; set; } = null!;        // rellenada por multi-mapping
}