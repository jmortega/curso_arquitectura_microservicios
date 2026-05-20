using AcademyManager.Application.Common.Interfaces;
using AcademyManager.Domain.Enrollments.Events;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Students.Events;
using AcademyManager.Domain.Subjects;
using MassTransit;
using MediatR;

namespace AcademyManager.Infrastructure.Messaging;

// ── Matrícula creada ──────────────────────────────────────────────────────────
public sealed class EnrollmentCreatedDomainEventHandler
    : INotificationHandler<EnrollmentCreatedEvent>
{
    private readonly IPublishEndpoint   _publishEndpoint;
    private readonly IStudentRepository _students;
    private readonly ISubjectRepository _subjects;

    public EnrollmentCreatedDomainEventHandler(
        IPublishEndpoint   publishEndpoint,
        IStudentRepository students,
        ISubjectRepository subjects)
    {
        _publishEndpoint = publishEndpoint;
        _students        = students;
        _subjects        = subjects;
    }

    public async Task Handle(
        EnrollmentCreatedEvent notification,
        CancellationToken      cancellationToken)
    {
        var student = await _students.GetByIdAsync(notification.StudentId, cancellationToken);
        var subject = await _subjects.GetByIdAsync(notification.SubjectId, cancellationToken);

        // Student.Name es un value object con FirstName y LastName
        var studentName = student is not null
            ? student.Name.FullName        // ← acceso correcto via value object
            : "Unknown";

        await _publishEndpoint.Publish(new AlumnoMatriculadoEvent(
            EnrollmentId: notification.EnrollmentId.Value,
            StudentId:    notification.StudentId.Value,
            SubjectId:    notification.SubjectId.Value,
            StudentName:  studentName,
            SubjectName:  subject?.Name ?? "Unknown",
            SubjectCode:  subject?.Code ?? "Unknown",
            EnrolledAt:   notification.EnrolledAt),
            cancellationToken);
    }
}

// ── Matrícula completada ──────────────────────────────────────────────────────
public sealed class EnrollmentCompletedDomainEventHandler
    : INotificationHandler<EnrollmentCompletedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EnrollmentCompletedDomainEventHandler(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public async Task Handle(
        EnrollmentCompletedEvent notification,
        CancellationToken        cancellationToken)
    {
        await _publishEndpoint.Publish(new EnrollmentCompletedIntegrationEvent(
            EnrollmentId: notification.EnrollmentId.Value,
            StudentId:    notification.StudentId.Value,
            SubjectId:    notification.SubjectId.Value,
            CompletedAt:  notification.CompletedAt),
            cancellationToken);
    }
}

// ── Matrícula cancelada ───────────────────────────────────────────────────────
public sealed class EnrollmentCancelledDomainEventHandler
    : INotificationHandler<EnrollmentCancelledEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public EnrollmentCancelledDomainEventHandler(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public async Task Handle(
        EnrollmentCancelledEvent notification,
        CancellationToken        cancellationToken)
    {
        await _publishEndpoint.Publish(new EnrollmentCancelledIntegrationEvent(
            EnrollmentId: notification.EnrollmentId.Value,
            StudentId:    notification.StudentId.Value,
            SubjectId:    notification.SubjectId.Value,
            CancelledAt:  notification.CancelledAt),
            cancellationToken);
    }
}

// ── Estudiante creado ─────────────────────────────────────────────────────────
public sealed class StudentCreatedDomainEventHandler
    : INotificationHandler<StudentCreatedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;

    public StudentCreatedDomainEventHandler(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public async Task Handle(
        StudentCreatedEvent notification,
        CancellationToken   cancellationToken)
    {
        // StudentCreatedEvent lleva FirstName y LastName directamente
        // (no el value object, sino los strings primitivos del evento)
        await _publishEndpoint.Publish(new StudentCreatedIntegrationEvent(
            StudentId:   notification.StudentId.Value,
            FirstName:   notification.FirstName,
            LastName:    notification.LastName,
            Email:       notification.Email,
            DateOfBirth: notification.DateOfBirth,
            CreatedAt:   notification.CreatedAt),
            cancellationToken);
    }
}
