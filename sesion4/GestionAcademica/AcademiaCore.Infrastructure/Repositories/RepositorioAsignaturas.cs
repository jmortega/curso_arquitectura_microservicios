// ── Infrastructure/Repositories/RepositorioAsignaturas.cs ────────────
namespace AcademiaCore.Infrastructure.Repositories;

using AcademiaCore.Domain.Entities;
using AcademiaCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class RepositorioAsignaturas : IRepositorioAsignaturas
{
    private readonly AcademiaDbContext _contexto;

    public RepositorioAsignaturas(AcademiaDbContext contexto)
        => _contexto = contexto;

    public async Task<Asignatura?> ObtenerAsync(
        int id, CancellationToken ct = default)
        => await _contexto.Asignaturas
                          .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<Asignatura>> ObtenerTodosAsync(
        CancellationToken ct = default)
        => await _contexto.Asignaturas
                          .Where(a => a.Activa)
                          .OrderBy(a => a.Nombre)
                          .ToListAsync(ct);

    public async Task<IEnumerable<Asignatura>> ObtenerPorIdsAsync(
        IEnumerable<int> ids, CancellationToken ct = default)
        => await _contexto.Asignaturas
                          .Where(a => ids.Contains(a.Id) && a.Activa)
                          .ToListAsync(ct);
}