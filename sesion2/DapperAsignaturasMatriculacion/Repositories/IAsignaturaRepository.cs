namespace DapperAsignaturas.Repositories;

public interface IAsignaturaRepository
{
    Task<IEnumerable<Asignatura>> ObtenerTodosAsync();
    Task<IEnumerable<Asignatura>> ObtenerTodos();
    Task<IEnumerable<dynamic>> ObtenerTodosAsyncDynamic();
    Task<Asignatura?> ObtenerPorIdAsync(int id);
    Task<IEnumerable<Asignatura>> BuscarPorNombreAsync(string nombre);
    Task<IEnumerable<Asignatura>> ObtenerPorCursoAsync(string curso);

    // MATRICULACIONES
    Task<IEnumerable<Matriculacion>> ObtenerMatriculacionesPorAlumnoAsync(int alumnoId);
    Task<int>                        MatricularAlumnoAsync(int alumnoId, int asignaturaId);

    Task<int> InsertarAsync(Asignatura asignatura);
    Task<bool> ActualizarAsync(Asignatura asignatura);
    Task<bool> EliminarAsync(int id);
    Task<int> ObtenerTotalAsignaturasAsync();
}