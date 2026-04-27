// ── Domain/Events/MatriculaCancelada.cs ──────────────────────────────
namespace AcademiaCore.Domain.Events;

using AcademiaCore.Domain.Common;

public record MatriculaCancelada(
    Guid   MatriculaId,
    int    EstudianteId,
    string PeriodoCodigo,
    string Motivo
) : IDomainEvent
{
    public Guid     EventId    { get; } = Guid.NewGuid();
    public DateTime OcurridoEn { get; } = DateTime.UtcNow;
}