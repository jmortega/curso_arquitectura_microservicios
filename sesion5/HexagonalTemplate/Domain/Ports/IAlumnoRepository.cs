namespace GestionAcademica.Domain.Ports;

using GestionAcademica.Domain.Entities;

/// <summary>
/// Puerto de salida: el dominio define QUÉ necesita.
/// La infraestructura implementa el CÓMO.
/// Equivalente a IUserRepository.cs de las imágenes.
/// </summary>
public interface IAlumnoRepository
{
    Task<Alumno?>             ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Alumno>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<bool>                ExisteAsync(int id, CancellationToken ct = default);
    Task                      AgregarAsync(Alumno alumno, CancellationToken ct = default);
    Task                      ActualizarAsync(Alumno alumno, CancellationToken ct = default);
}