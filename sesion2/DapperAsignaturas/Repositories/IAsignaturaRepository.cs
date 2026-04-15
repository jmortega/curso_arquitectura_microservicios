namespace DapperAsignaturas.Repositories;

public interface IAsignaturaRepository
{
    Task<IEnumerable<Asignatura>> ObtenerTodosAsync();
    Task<Asignatura?> ObtenerPorIdAsync(int id);
    Task<IEnumerable<Asignatura>> BuscarPorNombreAsync(string nombre);
    Task<IEnumerable<Asignatura>> ObtenerPorCursoAsync(string curso);
    Task<int> InsertarAsync(Asignatura asignatura);
    Task<bool> ActualizarAsync(Asignatura asignatura);
    Task<bool> EliminarAsync(int id);
    Task<int> ObtenerTotalAsignaturasAsync();
}