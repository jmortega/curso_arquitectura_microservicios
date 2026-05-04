using Enrollments.Domain.Events;
using Enrollments.Domain.Interfaces;

namespace Enrollments.Application.EventHandlers;

/// <summary>
/// PATRÓN OBSERVER — Suscriptor del evento StudentDeactivated.
///
/// Cuando el servicio de estudiantes publica que un alumno fue desactivado,
/// RabbitMQ lo encola y este handler cancela automáticamente todas sus
/// matrículas activas en este servicio.
///
/// Los dos servicios están completamente desacoplados:
///   - Students-service no conoce Enrollments-service
///   - La comunicación es asíncrona a través de RabbitMQ
/// </summary>
public sealed class StudentDeactivatedEventHandler
{
    private readonly IEnrollmentRepository _enrollRepo;
    private readonly ISubjectRepository    _subjectRepo;
    private readonly IEventPublisher       _events;
    private readonly ILogger<StudentDeactivatedEventHandler> _logger;

    public StudentDeactivatedEventHandler(
        IEnrollmentRepository  enrollRepo,
        ISubjectRepository     subjectRepo,
        IEventPublisher        events,
        ILogger<StudentDeactivatedEventHandler> logger)
    {
        _enrollRepo  = enrollRepo;
        _subjectRepo = subjectRepo;
        _events      = events;
        _logger      = logger;
    }

    public async Task HandleAsync(StudentDeactivatedEvent evt)
    {
        _logger.LogInformation(
            "[OBSERVER] StudentDeactivated recibido para {StudentId} ({Name})",
            evt.StudentId, evt.StudentName);

        var activeEnrollments = await _enrollRepo.GetActiveByStudentIdAsync(evt.StudentId);
        var count = 0;

        foreach (var enrollment in activeEnrollments)
        {
            enrollment.Cancel($"Alumno '{evt.StudentName}' desactivado automáticamente.");
            await _enrollRepo.UpdateAsync(enrollment);

            // Liberar plaza
            var subject = await _subjectRepo.GetByIdAsync(enrollment.SubjectId);
            if (subject is not null)
            {
                subject.DecrementEnrollments();
                await _subjectRepo.UpdateAsync(subject);
            }

            // Publicar evento de cancelación
            await _events.PublishAsync(new EnrollmentCancelledEvent(
                enrollment.Id, enrollment.StudentId, enrollment.SubjectId,
                $"Alumno desactivado: {evt.StudentName}"));

            count++;
        }

        _logger.LogInformation(
            "[OBSERVER] {Count} matrículas canceladas para el alumno desactivado {StudentId}",
            count, evt.StudentId);
    }
}
