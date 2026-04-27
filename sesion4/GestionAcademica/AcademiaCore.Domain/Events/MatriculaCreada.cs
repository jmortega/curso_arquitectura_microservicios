// ── Domain/Matriculacion/Events/MatriculaCreada.cs ───────────────────
namespace AcademiaCore.Domain.Matriculacion.Events;

using global::AcademiaCore.Domain.Common;

public record MatriculaCreada(
    Guid                MatriculaId,
    int                 EstudianteId,
    string              PeriodoCodigo,
    IReadOnlyList<string> NombresAsignaturas,
    int                 TotalCreditos
) : IDomainEvent
{
    public Guid     EventId    { get; } = Guid.NewGuid();
    public DateTime OcurridoEn { get; } = DateTime.UtcNow;
}