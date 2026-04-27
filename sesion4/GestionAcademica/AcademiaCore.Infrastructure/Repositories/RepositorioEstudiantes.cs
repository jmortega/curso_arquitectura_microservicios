// ── Infrastructure/Repositories/RepositorioEstudiantes.cs ────────────
namespace AcademiaCore.Infrastructure.Repositories;

using AcademiaCore.Domain.Entities;
using AcademiaCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class RepositorioEstudiantes : IRepositorioEstudiantes
{
    private readonly AcademiaDbContext _contexto;

    public RepositorioEstudiantes(AcademiaDbContext contexto)
        => _contexto = contexto;

    public async Task<Estudiante?> ObtenerAsync(
        int id, CancellationToken ct = default)
        => await _contexto.Estudiantes
                          .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IEnumerable<Estudiante>> ObtenerTodosAsync(
        CancellationToken ct = default)
        => await _contexto.Estudiantes
                          .Where(e => e.Activo)
                          .OrderBy(e => e.Apellidos)
                          .ToListAsync(ct);
                          
    public async Task<bool> ExisteAsync(
        int id, CancellationToken ct = default)
        => await _contexto.Estudiantes
                          .AnyAsync(e => e.Id == id, ct);
}