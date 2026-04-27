// ── Application/ServicioMatriculacion.cs ─────────────────────────────
namespace AcademiaCore.Application;

using AcademiaCore.Domain.Aggregates;
using AcademiaCore.Domain.Exceptions;
using AcademiaCore.Domain.Matriculacion.ValueObjects;
using AcademiaCore.Domain.Repositories;

/// <summary>
/// Orquesta los casos de uso de matriculación.
/// Coordina repositorios, valida precondiciones y despacha
/// los eventos de dominio generados por el agregado.
/// </summary>
public class ServicioMatriculacion
{
    private readonly IRepositorioMatriculas  _repoMatriculas;
    private readonly IRepositorioEstudiantes _repoEstudiantes;
    private readonly IRepositorioAsignaturas _repoAsignaturas;
    private readonly IServicioEventos        _servicioEventos;

    public ServicioMatriculacion(
        IRepositorioMatriculas  repoMatriculas,
        IRepositorioEstudiantes repoEstudiantes,
        IRepositorioAsignaturas repoAsignaturas,
        IServicioEventos        servicioEventos)
    {
        _repoMatriculas  = repoMatriculas;
        _repoEstudiantes = repoEstudiantes;
        _repoAsignaturas = repoAsignaturas;
        _servicioEventos = servicioEventos;
    }

    // ── Caso de uso: Matricular estudiante ────────────────────────────
    public async Task<Matricula> MatricularAsync(
        int        estudianteId,
        int        anyo,
        int        semestre,
        List<int>  idsAsignaturas,
        CancellationToken ct = default)
    {
        // 1. Verificar que el estudiante existe y está activo
        var estudiante = await _repoEstudiantes.ObtenerAsync(estudianteId, ct)
                      ?? throw new KeyNotFoundException(
                             $"Estudiante {estudianteId} no encontrado.");

        if (!estudiante.Activo)
            throw new MatriculacionException(
                "El estudiante no está activo.");

        // 2. Verificar que no hay una matrícula activa en el mismo periodo
        var periodo = PeriodoAcademico.Crear(anyo, semestre);

        var yaExiste = await _repoMatriculas
            .ObtenerActivaAsync(estudianteId, periodo, ct);

        if (yaExiste is not null)
            throw new MatriculacionException(
                $"Ya existe una matrícula activa para el periodo {periodo.Codigo}.");

        // 3. Cargar las asignaturas solicitadas
        var asignaturas = (await _repoAsignaturas
            .ObtenerPorIdsAsync(idsAsignaturas, ct)).ToList();

        var noEncontradas = idsAsignaturas
            .Except(asignaturas.Select(a => a.Id))
            .ToList();

        if (noEncontradas.Count > 0)
            throw new KeyNotFoundException(
                $"Asignaturas no encontradas: {string.Join(", ", noEncontradas)}.");

        // 4. Crear el agregado — las invariantes se validan dentro del dominio
        var matricula = Matricula.Crear(estudianteId, periodo, asignaturas);

        // 5. Persistir y despachar eventos
        await _repoMatriculas.AgregarAsync(matricula, ct);
        await _servicioEventos.DespacharAsync(matricula.EventosDominio, ct);
        matricula.LimpiarEventos();

        return matricula;
    }

    // ── Caso de uso: Cancelar matrícula ──────────────────────────────
    public async Task CancelarAsync(
        Guid   matriculaId,
        string motivo,
        CancellationToken ct = default)
    {
        var matricula = await _repoMatriculas.ObtenerAsync(matriculaId, ct)
                     ?? throw new KeyNotFoundException(
                            $"Matrícula {matriculaId} no encontrada.");

        // Cargar las asignaturas para liberar sus plazas
        var idsAsignaturas = matricula.Lineas.Select(l => l.AsignaturaId).ToList();
        var asignaturas    = (await _repoAsignaturas
            .ObtenerPorIdsAsync(idsAsignaturas, ct)).ToList();

        // El agregado valida que se puede cancelar y registra el evento
        matricula.Cancelar(motivo, asignaturas);

        await _repoMatriculas.ActualizarAsync(matricula, ct);
        await _servicioEventos.DespacharAsync(matricula.EventosDominio, ct);
        matricula.LimpiarEventos();
    }

    // ── Caso de uso: Consultar matrículas de un estudiante ────────────
    public async Task<IEnumerable<Matricula>> ObtenerPorEstudianteAsync(
        int estudianteId,
        CancellationToken ct = default)
    {
        if (!await _repoEstudiantes.ExisteAsync(estudianteId, ct))
            throw new KeyNotFoundException(
                $"Estudiante {estudianteId} no encontrado.");

        return await _repoMatriculas
            .ObtenerPorEstudianteAsync(estudianteId, ct);
    }
}