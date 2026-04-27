// ── Application/IServicioEventos.cs ──────────────────────────────────
namespace AcademiaCore.Application;

using AcademiaCore.Domain.Common;

public interface IServicioEventos
{
    Task DespacharAsync(IReadOnlyList<IDomainEvent> eventos,
                        CancellationToken ct = default);
}