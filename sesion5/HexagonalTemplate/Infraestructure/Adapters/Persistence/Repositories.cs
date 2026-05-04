namespace GestionAcademica.Infrastructure.Adapters.Persistence;

using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Ports;
using Microsoft.EntityFrameworkCore;

// ── Implementación SQL: AlumnoRepository ──────────────────────────────
/// <summary>
/// Adaptador secundario de persistencia.
/// Implementa el puerto IAlumnoRepository usando EF Core + SQLite.
/// El dominio no sabe que existe EF Core.
/// </summary>
public class AlumnoRepository : IAlumnoRepository
{
    private readonly AcademiaDbContext _ctx;

    public AlumnoRepository(AcademiaDbContext ctx) => _ctx = ctx;

    public async Task<Alumno?> ObtenerPorIdAsync(int id, CancellationToken ct)
        => await _ctx.Alumnos.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<Alumno>> ObtenerTodosAsync(CancellationToken ct)
        => await _ctx.Alumnos
                     .Where(a => a.Activo)
                     .OrderBy(a => a.Apellidos)
                     .ToListAsync(ct);

    public async Task<bool> ExisteAsync(int id, CancellationToken ct)
        => await _ctx.Alumnos.AnyAsync(a => a.Id == id, ct);

    public async Task AgregarAsync(Alumno alumno, CancellationToken ct)
    {
        await _ctx.Alumnos.AddAsync(alumno, ct);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task ActualizarAsync(Alumno alumno, CancellationToken ct)
    {
        _ctx.Alumnos.Update(alumno);
        await _ctx.SaveChangesAsync(ct);
    }
}

// ── Implementación SQL: AsignaturaRepository ──────────────────────────
public class AsignaturaRepository : IAsignaturaRepository
{
    private readonly AcademiaDbContext _ctx;

    public AsignaturaRepository(AcademiaDbContext ctx) => _ctx = ctx;

    public async Task<Asignatura?> ObtenerPorIdAsync(int id, CancellationToken ct)
        => await _ctx.Asignaturas.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<Asignatura>> ObtenerTodasAsync(CancellationToken ct)
        => await _ctx.Asignaturas
                     .Where(a => a.Activa)
                     .OrderBy(a => a.Nombre)
                     .ToListAsync(ct);

    public async Task AgregarAsync(Asignatura asignatura, CancellationToken ct)
    {
        await _ctx.Asignaturas.AddAsync(asignatura, ct);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task ActualizarAsync(Asignatura asignatura, CancellationToken ct)
    {
        _ctx.Asignaturas.Update(asignatura);
        await _ctx.SaveChangesAsync(ct);
    }
}

// ── Implementación SQL: MatriculaRepository ───────────────────────────
public class MatriculaRepository : IMatriculaRepository
{
    private readonly AcademiaDbContext _ctx;

    public MatriculaRepository(AcademiaDbContext ctx) => _ctx = ctx;

    public async Task<Matricula?> ObtenerPorIdAsync(int id, CancellationToken ct)
        => await _ctx.Matriculas
                     .Include(m => m.Alumno)
                     .Include(m => m.Asignatura)
                     .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IEnumerable<Matricula>> ObtenerPorAlumnoAsync(
        int alumnoId, CancellationToken ct)
        => await _ctx.Matriculas
                     .Include(m => m.Asignatura)
                     .Where(m => m.AlumnoId == alumnoId)
                     .OrderByDescending(m => m.FechaAlta)
                     .ToListAsync(ct);

    public async Task<bool> ExisteMatriculaActivaAsync(
        int alumnoId, int asignaturaId, string periodo, CancellationToken ct)
        => await _ctx.Matriculas.AnyAsync(m =>
            m.AlumnoId     == alumnoId    &&
            m.AsignaturaId == asignaturaId &&
            m.Periodo      == periodo      &&
            m.Activa, ct);

    public async Task AgregarAsync(Matricula matricula, CancellationToken ct)
    {
        await _ctx.Matriculas.AddAsync(matricula, ct);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task ActualizarAsync(Matricula matricula, CancellationToken ct)
    {
        _ctx.Matriculas.Update(matricula);
        await _ctx.SaveChangesAsync(ct);
    }
}