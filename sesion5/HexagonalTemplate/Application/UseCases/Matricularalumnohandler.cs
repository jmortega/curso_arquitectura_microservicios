namespace GestionAcademica.Application.UseCases;

using GestionAcademica.Application.DTOs;
using GestionAcademica.Domain.Ports;
using GestionAcademica.Domain.Services;

/// <summary>
/// Caso de uso: MatricularAlumnoHandler.
/// Orquesta el flujo de datos desde y hacia el dominio.
/// Solo conoce puertos (interfaces) — nunca implementaciones concretas.
/// </summary>
public class MatricularAlumnoHandler
{
    private readonly ServicioMatriculacion _servicio;
    private readonly IMatriculaRepository  _matriculaRepo;
    private readonly INotificacionService  _notificacion;

    public MatricularAlumnoHandler(
        ServicioMatriculacion servicio,
        IMatriculaRepository  matriculaRepo,
        INotificacionService  notificacion)
    {
        _servicio      = servicio;
        _matriculaRepo = matriculaRepo;
        _notificacion  = notificacion;
    }

    public async Task<MatriculaDto> EjecutarAsync(
        MatricularAlumnoRequest request,
        CancellationToken       ct = default)
    {
        // Delegar la lógica de negocio al Domain Service
        var matricula = await _servicio.MatricularAsync(
            request.AlumnoId,
            request.AsignaturaId,
            request.Periodo,
            ct);

        // Notificar al alumno (puerto de salida)
        await _notificacion.EnviarEmailAsync(
            destinatario: $"alumno_{request.AlumnoId}@academia.es",
            asunto:       "Matrícula confirmada",
            cuerpo:       $"Se ha confirmado su matrícula en el periodo {request.Periodo}.",
            ct:           ct);

        // Construir y devolver el DTO de respuesta
        return new MatriculaDto(
            Id:               matricula.Id,
            AlumnoId:         matricula.AlumnoId,
            NombreAlumno:     $"Alumno {matricula.AlumnoId}",
            AsignaturaId:     matricula.AsignaturaId,
            NombreAsignatura: $"Asignatura {matricula.AsignaturaId}",
            Periodo:          matricula.Periodo,
            FechaAlta:        matricula.FechaAlta,
            Activa:           matricula.Activa);
    }
}