// ── Infrastructure/ServicioEventosLog.cs ─────────────────────────────
namespace AcademiaCore.Infrastructure;

using AcademiaCore.Application;
using AcademiaCore.Domain.Common;
using AcademiaCore.Domain.Events;
using AcademiaCore.Domain.Matriculacion.Events;

/// <summary>
/// Implementación simple que registra los eventos en el log.
/// En producción se reemplazaría por una implementación
/// con un bus de mensajes real.
/// </summary>
public class ServicioEventosLog : IServicioEventos
{
    private readonly ILogger<ServicioEventosLog> _logger;

    public ServicioEventosLog(ILogger<ServicioEventosLog> logger)
        => _logger = logger;

    public async Task DespacharAsync(
        IReadOnlyList<IDomainEvent> eventos,
        CancellationToken ct = default)
    {
        foreach (var evento in eventos)
        {
            switch (evento)
            {
                case MatriculaCreada e:
                    _logger.LogInformation(
                        "[EVENTO] MatriculaCreada — Estudiante: {EstudianteId}, " +
                        "Periodo: {Periodo}, Créditos: {Creditos}",
                        e.EstudianteId, e.PeriodoCodigo, e.TotalCreditos);
                    break;

                case MatriculaCancelada e:
                    _logger.LogInformation(
                        "[EVENTO] MatriculaCancelada — Matricula: {MatriculaId}, " +
                        "Motivo: {Motivo}",
                        e.MatriculaId, e.Motivo);
                    break;

                default:
                    _logger.LogWarning(
                        "[EVENTO] Tipo no manejado: {Tipo}", evento.GetType().Name);
                    break;
            }
        }

        await Task.CompletedTask;
    }
}