// ── Infrastructure/Repositories/RepositorioMatriculas.cs ─────────────
namespace AcademiaCore.Infrastructure.Repositories;

using AcademiaCore.Domain.Aggregates;
using AcademiaCore.Domain.Matriculacion.ValueObjects;
using AcademiaCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

public class RepositorioMatriculas : IRepositorioMatriculas
{
    private readonly AcademiaDbContext _contexto;

    public RepositorioMatriculas(AcademiaDbContext contexto)
        => _contexto = contexto;

    public async Task<Matricula?> ObtenerAsync(
        Guid id, CancellationToken ct = default)
        => await _contexto.Matriculas
                          .Include(m => m.Lineas)
                          .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IEnumerable<Matricula>> ObtenerPorEstudianteAsync(
        int estudianteId, CancellationToken ct = default)
        => await _contexto.Matriculas
                          .Include(m => m.Lineas)
                          .Where(m => m.EstudianteId == estudianteId)
                          .OrderByDescending(m => m.FechaCreacion)
                          .ToListAsync(ct);

    public async Task<Matricula?> ObtenerActivaAsync(
        int estudianteId,
        PeriodoAcademico periodo,
        CancellationToken ct = default)
        => await _contexto.Matriculas
                          .Include(m => m.Lineas)
                          .FirstOrDefaultAsync(m =>
                              m.EstudianteId      == estudianteId        &&
                              m.Estado            == EstadoMatricula.Activa &&
                              m.Periodo.Anyo      == periodo.Anyo        &&
                              m.Periodo.Semestre  == periodo.Semestre,
                              ct);

    public async Task AgregarAsync(
        Matricula matricula, CancellationToken ct = default)
    {
        await _contexto.Matriculas.AddAsync(matricula, ct);
        await _contexto.SaveChangesAsync(ct);
    }

    public async Task ActualizarAsync(
        Matricula matricula, CancellationToken ct = default)
    {
        _contexto.Matriculas.Update(matricula);
        await _contexto.SaveChangesAsync(ct);
    }
}