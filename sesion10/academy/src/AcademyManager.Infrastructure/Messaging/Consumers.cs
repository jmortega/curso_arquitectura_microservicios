using AcademyManager.Application.Common.Interfaces;
using AcademyManager.Application.ReadModels;
using MassTransit;

namespace AcademyManager.Infrastructure.Messaging;

// ── Contratos de integración ──────────────────────────────────────────────────
public record AlumnoMatriculadoEvent(
    Guid     EnrollmentId,
    Guid     StudentId,
    Guid     SubjectId,
    string   StudentName,
    string   SubjectName,
    string   SubjectCode,
    DateTime EnrolledAt);

public record EnrollmentCompletedIntegrationEvent(
    Guid     EnrollmentId,
    Guid     StudentId,
    Guid     SubjectId,
    DateTime CompletedAt);

public record EnrollmentCancelledIntegrationEvent(
    Guid     EnrollmentId,
    Guid     StudentId,
    Guid     SubjectId,
    DateTime CancelledAt);

public record StudentCreatedIntegrationEvent(
    Guid     StudentId,
    string   FirstName,
    string   LastName,
    string   Email,
    DateTime DateOfBirth,
    DateTime CreatedAt);

// ── Consumer: matrícula creada → actualiza MongoDB ────────────────────────────
public sealed class AlumnoMatriculadoConsumer : IConsumer<AlumnoMatriculadoEvent>
{
    private readonly IStudentReadRepository    _students;
    private readonly ISubjectReadRepository    _subjects;
    private readonly IEnrollmentReadRepository _enrollments;

    public AlumnoMatriculadoConsumer(
        IStudentReadRepository    students,
        ISubjectReadRepository    subjects,
        IEnrollmentReadRepository enrollments)
    {
        _students    = students;
        _subjects    = subjects;
        _enrollments = enrollments;
    }

    public async Task Consume(ConsumeContext<AlumnoMatriculadoEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        // 1. Upsert del enrollment en MongoDB
        await _enrollments.UpsertAsync(new EnrollmentReadModel
        {
            Id           = msg.EnrollmentId,
            StudentId    = msg.StudentId,
            SubjectId    = msg.SubjectId,
            StudentName  = msg.StudentName,
            StudentEmail = string.Empty,   // no disponible en el evento; se puede enriquecer si se necesita
            SubjectName  = msg.SubjectName,
            SubjectCode  = msg.SubjectCode,
            EnrolledAt   = msg.EnrolledAt,
            Status       = "Active"
        }, ct);

        // 2. Añadir resumen al documento del estudiante
        await _students.AddEnrollmentSummaryAsync(msg.StudentId,
            new StudentReadModel.EnrollmentSummary
            {
                EnrollmentId = msg.EnrollmentId,
                SubjectId    = msg.SubjectId,
                SubjectName  = msg.SubjectName,
                SubjectCode  = msg.SubjectCode,
                EnrolledAt   = msg.EnrolledAt,
                Status       = "Active"
            }, ct);

        // 3. Incrementar contador de alumnos en la asignatura
        await _subjects.IncrementEnrolledStudentsAsync(msg.SubjectId, 1, ct);
    }
}

// ── Consumer: matrícula completada → actualiza estado en MongoDB ──────────────
//
//  IEnrollmentReadRepository no tiene GetByIdAsync, solo GetByStudentIdAsync.
//  Se recupera la lista del estudiante y se filtra por EnrollmentId.
//
public sealed class EnrollmentCompletedConsumer
    : IConsumer<EnrollmentCompletedIntegrationEvent>
{
    private readonly IEnrollmentReadRepository _enrollments;

    public EnrollmentCompletedConsumer(IEnrollmentReadRepository enrollments)
        => _enrollments = enrollments;

    public async Task Consume(ConsumeContext<EnrollmentCompletedIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var studentEnrollments = await _enrollments.GetByStudentIdAsync(msg.StudentId, ct);
        var enrollment = studentEnrollments.FirstOrDefault(e => e.Id == msg.EnrollmentId);
        if (enrollment is null) return;

        enrollment.Status      = "Completed";
        enrollment.CompletedAt = msg.CompletedAt;
        await _enrollments.UpsertAsync(enrollment, ct);
    }
}

// ── Consumer: matrícula cancelada → actualiza estado en MongoDB ───────────────
public sealed class EnrollmentCancelledConsumer
    : IConsumer<EnrollmentCancelledIntegrationEvent>
{
    private readonly IEnrollmentReadRepository _enrollments;
    private readonly ISubjectReadRepository    _subjects;

    public EnrollmentCancelledConsumer(
        IEnrollmentReadRepository enrollments,
        ISubjectReadRepository    subjects)
    {
        _enrollments = enrollments;
        _subjects    = subjects;
    }

    public async Task Consume(ConsumeContext<EnrollmentCancelledIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var studentEnrollments = await _enrollments.GetByStudentIdAsync(msg.StudentId, ct);
        var enrollment = studentEnrollments.FirstOrDefault(e => e.Id == msg.EnrollmentId);
        if (enrollment is null) return;

        enrollment.Status      = "Cancelled";
        enrollment.CompletedAt = msg.CancelledAt;
        await _enrollments.UpsertAsync(enrollment, ct);

        await _subjects.IncrementEnrolledStudentsAsync(msg.SubjectId, -1, ct);
    }
}

// ── Consumer: estudiante creado → crea documento en MongoDB ──────────────────
public sealed class StudentCreatedConsumer
    : IConsumer<StudentCreatedIntegrationEvent>
{
    private readonly IStudentReadRepository _students;

    public StudentCreatedConsumer(IStudentReadRepository students)
        => _students = students;

    public async Task Consume(ConsumeContext<StudentCreatedIntegrationEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        await _students.UpsertAsync(new StudentReadModel
        {
            Id          = msg.StudentId,
            FirstName   = msg.FirstName,
            LastName    = msg.LastName,
            FullName    = $"{msg.FirstName} {msg.LastName}",
            Email       = msg.Email,
            DateOfBirth = msg.DateOfBirth,
            CreatedAt   = msg.CreatedAt,
            Enrollments = new List<StudentReadModel.EnrollmentSummary>()
            // EnrollmentNumber no existe en StudentReadModel → no se asigna
        }, ct);
    }
}
