namespace DapperAsignaturas;

public class Asignatura
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }

    public override string ToString()
    {
        return $"""
                ┌─────────────────────────────────────────┐
                  ID:          {Id}
                  Nombre:      {Nombre}
                  Descripción: {Descripcion}
                  Curso:       {Curso}
                  Fecha Alta:  {FechaAlta:dd/MM/yyyy}
                └─────────────────────────────────────────┘
                """;
    }
}