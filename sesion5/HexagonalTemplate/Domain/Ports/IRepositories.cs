namespace GestionAcademica.Domain.Ports;

using GestionAcademica.Domain.Entities;

// ── Puerto: Asignaturas ───────────────────────────────────────────────
public interface IAsignaturaRepository
{
    Task<Asignatura?>             ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Asignatura>> ObtenerTodasAsync(CancellationToken ct = default);
    Task                          AgregarAsync(Asignatura asignatura, CancellationToken ct = default);
    Task                          ActualizarAsync(Asignatura asignatura, CancellationToken ct = default);
}

// ── Puerto: Matrículas ────────────────────────────────────────────────
public interface IMatriculaRepository
{
    Task<Matricula?>             ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Matricula>> ObtenerPorAlumnoAsync(int alumnoId, CancellationToken ct = default);
    Task<bool>                   ExisteMatriculaActivaAsync(int alumnoId, int asignaturaId,
                                     string periodo, CancellationToken ct = default);
    Task                         AgregarAsync(Matricula matricula, CancellationToken ct = default);
    Task                         ActualizarAsync(Matricula matricula, CancellationToken ct = default);
}

// ── Puerto: Notificaciones externas ───────────────────────────────────
public interface INotificacionService
{
    Task EnviarEmailAsync(string destinatario, string asunto,
                           string cuerpo, CancellationToken ct = default);
}