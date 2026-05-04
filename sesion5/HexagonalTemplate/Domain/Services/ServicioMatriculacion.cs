namespace GestionAcademica.Domain.Services;

using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Ports;

/// <summary>
/// Domain Service: lógica de negocio que involucra varias entidades
/// y no pertenece a ninguna entidad concreta.
/// Solo depende de interfaces (Ports) — sin infraestructura.
/// </summary>
public class ServicioMatriculacion
{
    private readonly IAlumnoRepository    _alumnoRepo;
    private readonly IAsignaturaRepository _asignaturaRepo;
    private readonly IMatriculaRepository  _matriculaRepo;

    public ServicioMatriculacion(
        IAlumnoRepository    alumnoRepo,
        IAsignaturaRepository asignaturaRepo,
        IMatriculaRepository  matriculaRepo)
    {
        _alumnoRepo     = alumnoRepo;
        _asignaturaRepo = asignaturaRepo;
        _matriculaRepo  = matriculaRepo;
    }

    /// <summary>
    /// Regla de negocio: un alumno no puede matricularse dos veces
    /// en la misma asignatura en el mismo periodo.
    /// </summary>
    public async Task<Matricula> MatricularAsync(
        int    alumnoId,
        int    asignaturaId,
        string periodo,
        CancellationToken ct = default)
    {
        // Verificar que el alumno existe y está activo
        var alumno = await _alumnoRepo.ObtenerPorIdAsync(alumnoId, ct)
            ?? throw new KeyNotFoundException(
                   $"No existe el alumno con ID {alumnoId}.");

        if (!alumno.Activo)
            throw new InvalidOperationException(
                "No se puede matricular a un alumno inactivo.");

        // Verificar que la asignatura existe y está activa
        var asignatura = await _asignaturaRepo.ObtenerPorIdAsync(asignaturaId, ct)
            ?? throw new KeyNotFoundException(
                   $"No existe la asignatura con ID {asignaturaId}.");

        if (!asignatura.Activa)
            throw new InvalidOperationException(
                "No se puede matricular en una asignatura inactiva.");

        // Regla de negocio: no duplicar matrícula en el mismo periodo
        bool yaMatriculado = await _matriculaRepo
            .ExisteMatriculaActivaAsync(alumnoId, asignaturaId, periodo, ct);

        if (yaMatriculado)
            throw new InvalidOperationException(
                $"El alumno ya está matriculado en '{asignatura.Nombre}' " +
                $"para el periodo {periodo}.");

        // Crear la matrícula — la lógica de creación vive en la entidad
        var matricula = Matricula.Crear(alumnoId, asignaturaId, periodo);

        await _matriculaRepo.AgregarAsync(matricula, ct);

        return matricula;
    }
}