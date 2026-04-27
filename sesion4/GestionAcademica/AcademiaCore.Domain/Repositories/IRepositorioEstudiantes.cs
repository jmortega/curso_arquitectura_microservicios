// ── Domain/Repositories/IRepositorioEstudiantes.cs ───────────────────
namespace AcademiaCore.Domain.Repositories;

using AcademiaCore.Domain.Entities;


public interface IRepositorioEstudiantes
{
    Task<Estudiante?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Estudiante>> ObtenerTodosAsync(CancellationToken ct = default);  // ← añadir
   
    Task<bool>        ExisteAsync(int id, CancellationToken ct = default);
}