namespace GestionAcademica.Application.UseCases;

using GestionAcademica.Application.DTOs;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Ports;

// ── Caso de uso: Obtener todos los alumnos ────────────────────────────
public class ObtenerAlumnosHandler
{
    private readonly IAlumnoRepository _repo;

    public ObtenerAlumnosHandler(IAlumnoRepository repo) => _repo = repo;

    public async Task<IEnumerable<AlumnoDto>> EjecutarAsync(
        CancellationToken ct = default)
    {
        var alumnos = await _repo.ObtenerTodosAsync(ct);

        return alumnos.Select(a => new AlumnoDto(
            a.Id, a.NombreCompleto, a.Nombre, a.Apellidos, a.Email, a.Activo));
    }
}

// ── Caso de uso: Obtener alumno por ID ────────────────────────────────
public class ObtenerAlumnoPorIdHandler
{
    private readonly IAlumnoRepository _repo;

    public ObtenerAlumnoPorIdHandler(IAlumnoRepository repo) => _repo = repo;

    public async Task<AlumnoDto?> EjecutarAsync(int id,
                                                  CancellationToken ct = default)
    {
        var alumno = await _repo.ObtenerPorIdAsync(id, ct);

        if (alumno is null) return null;

        return new AlumnoDto(
            alumno.Id, alumno.NombreCompleto,
            alumno.Nombre, alumno.Apellidos,
            alumno.Email, alumno.Activo);
    }
}

// ── Caso de uso: Crear alumno ─────────────────────────────────────────
public class CrearAlumnoHandler
{
    private readonly IAlumnoRepository _repo;
    private static   int               _nextId = 10; // Simulado

    public CrearAlumnoHandler(IAlumnoRepository repo) => _repo = repo;

    public async Task<AlumnoDto> EjecutarAsync(CrearAlumnoRequest request,
                                                CancellationToken ct = default)
    {
        // El dominio valida las reglas de negocio
        var alumno = Alumno.Crear(++_nextId, request.Nombre,
                                   request.Apellidos, request.Email);

        await _repo.AgregarAsync(alumno, ct);

        return new AlumnoDto(
            alumno.Id, alumno.NombreCompleto,
            alumno.Nombre, alumno.Apellidos,
            alumno.Email, alumno.Activo);
    }
}

// ── Caso de uso: Obtener matrículas de un alumno ──────────────────────
public class ObtenerMatriculasAlumnoHandler
{
    private readonly IMatriculaRepository  _matriculaRepo;
    private readonly IAlumnoRepository     _alumnoRepo;
    private readonly IAsignaturaRepository _asignaturaRepo;

    public ObtenerMatriculasAlumnoHandler(
        IMatriculaRepository  matriculaRepo,
        IAlumnoRepository     alumnoRepo,
        IAsignaturaRepository asignaturaRepo)
    {
        _matriculaRepo  = matriculaRepo;
        _alumnoRepo     = alumnoRepo;
        _asignaturaRepo = asignaturaRepo;
    }

    public async Task<IEnumerable<MatriculaDto>> EjecutarAsync(
        int alumnoId, CancellationToken ct = default)
    {
        var alumno = await _alumnoRepo.ObtenerPorIdAsync(alumnoId, ct)
            ?? throw new KeyNotFoundException($"Alumno {alumnoId} no encontrado.");

        var matriculas = await _matriculaRepo.ObtenerPorAlumnoAsync(alumnoId, ct);

        var resultado = new List<MatriculaDto>();

        foreach (var m in matriculas)
        {
            var asignatura = await _asignaturaRepo.ObtenerPorIdAsync(m.AsignaturaId, ct);

            resultado.Add(new MatriculaDto(
                m.Id, m.AlumnoId, alumno.NombreCompleto,
                m.AsignaturaId, asignatura?.Nombre ?? "Desconocida",
                m.Periodo, m.FechaAlta, m.Activa));
        }

        return resultado;
    }
}

// ── Caso de uso: Obtener todas las asignaturas ────────────────────────
public class ObtenerAsignaturasHandler
{
    private readonly IAsignaturaRepository _repo;

    public ObtenerAsignaturasHandler(IAsignaturaRepository repo) => _repo = repo;

    public async Task<IEnumerable<AsignaturaDto>> EjecutarAsync(
        CancellationToken ct = default)
    {
        var asignaturas = await _repo.ObtenerTodasAsync(ct);

        return asignaturas.Select(a => new AsignaturaDto(
            a.Id, a.Codigo, a.Nombre, a.Creditos, a.Activa));
    }
}

// ── Caso de uso: Desactivar asignatura ────────────────────────────────
public class DesactivarAsignaturaHandler
{
    private readonly IAsignaturaRepository _repo;
 
    public DesactivarAsignaturaHandler(IAsignaturaRepository repo)
        => _repo = repo;
 
    public async Task<AsignaturaDto> EjecutarAsync(int id,
                                                    CancellationToken ct = default)
    {
        // 1. Obtener la asignatura — incluidas las inactivas
        var asignatura = await _repo.ObtenerPorIdAsync(id, ct)
            ?? throw new KeyNotFoundException(
                   $"No existe la asignatura con ID {id}.");
 
        // 2. El dominio aplica la regla de negocio (lanza si ya está inactiva)
        asignatura.Desactivar();
 
        // 3. Persistir el cambio a través del puerto
        await _repo.ActualizarAsync(asignatura, ct);
 
        return new AsignaturaDto(
            asignatura.Id,
            asignatura.Codigo,
            asignatura.Nombre,
            asignatura.Creditos,
            asignatura.Activa);
    }
}