// ── Domain/Repositories/IRepositorioMatriculas.cs ────────────────────
namespace AcademiaCore.Domain.Repositories;

using AcademiaCore.Domain.Aggregates;
using AcademiaCore.Domain.Matriculacion.ValueObjects;

public interface IRepositorioMatriculas
{
    Task<Matricula?>              ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Matricula>>  ObtenerPorEstudianteAsync(int estudianteId, CancellationToken ct = default);
    Task<Matricula?>              ObtenerActivaAsync(int estudianteId, PeriodoAcademico periodo, CancellationToken ct = default);
    Task                          AgregarAsync(Matricula matricula, CancellationToken ct = default);
    Task                          ActualizarAsync(Matricula matricula, CancellationToken ct = default);
}