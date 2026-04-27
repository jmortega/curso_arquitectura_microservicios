namespace AcademiaCore.Domain.Repositories;

using AcademiaCore.Domain.Entities;

public interface IRepositorioAsignaturas
{
    Task<Asignatura?>             ObtenerAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Asignatura>> ObtenerTodosAsync(CancellationToken ct = default);  // ← añadir
    
    Task<IEnumerable<Asignatura>> ObtenerPorIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
}