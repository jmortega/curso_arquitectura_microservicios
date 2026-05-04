using Enrollments.Application.DTOs;
using Enrollments.Domain.Events;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.Factories;
using Enrollments.Domain.Interfaces;
using Enrollments.Domain.Strategies;
using FluentValidation;
using MediatR;

namespace Enrollments.Application.Commands.EnrollStudent;

// ── Command ───────────────────────────────────────────────────────────────────
public record EnrollStudentCommand(
    Guid    StudentId,
    Guid    SubjectId,
    string? Notes
) : IRequest<EnrollmentDto>;

public sealed class EnrollStudentCommandValidator : AbstractValidator<EnrollStudentCommand>
{
    public EnrollStudentCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
/// <summary>
/// Orquesta el caso de uso de matriculación combinando:
/// - STRATEGY:  Valida usando las estrategias registradas
/// - FACTORY:   Crea la entidad Enrollment
/// - OBSERVER:  Publica evento EnrollmentCreated a RabbitMQ
/// - MEDIATOR:  Este handler es invocado por MediatR, desacoplando el Controller
/// </summary>
public sealed class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, EnrollmentDto>
{
    private readonly IEnrollmentRepository            _enrollRepo;
    private readonly ISubjectRepository               _subjectRepo;
    private readonly IStudentServiceClient            _studentClient;
    private readonly IEventPublisher                  _events;
    private readonly IEnumerable<IEnrollmentValidationStrategy> _strategies;

    public EnrollStudentCommandHandler(
        IEnrollmentRepository            enrollRepo,
        ISubjectRepository               subjectRepo,
        IStudentServiceClient            studentClient,
        IEventPublisher                  events,
        IEnumerable<IEnrollmentValidationStrategy> strategies)
    {
        _enrollRepo    = enrollRepo;
        _subjectRepo   = subjectRepo;
        _studentClient = studentClient;
        _events        = events;
        _strategies    = strategies;
    }

    public async Task<EnrollmentDto> Handle(EnrollStudentCommand cmd, CancellationToken ct)
    {
        // 1. Obtener asignatura
        var subject = await _subjectRepo.GetByIdAsync(cmd.SubjectId)
            ?? throw new SubjectNotFoundException(cmd.SubjectId);

        // 2. Verificar que el alumno existe en el servicio de estudiantes (HTTP)
        var studentInfo = await _studentClient.GetStudentAsync(cmd.StudentId)
            ?? throw new StudentNotFoundException(cmd.StudentId);

        // 3. Verificar matrícula duplicada
        var existing = await _enrollRepo.GetActiveByStudentAndSubjectAsync(cmd.StudentId, cmd.SubjectId);
        if (existing is not null)
            throw new DuplicateEnrollmentException(cmd.StudentId, cmd.SubjectId);

        // 4. PATRÓN STRATEGY: ejecutar todas las estrategias de validación
        var context = new EnrollmentValidationContext(cmd.StudentId, subject, studentInfo);
        foreach (var strategy in _strategies)
        {
            var result = await strategy.ValidateAsync(context);
            if (!result.IsValid)
                throw new EnrollmentValidationException(strategy.Name, result.ErrorMessage!);
        }

        // 5. PATRÓN FACTORY: construir la entidad de dominio
        var enrollment = EnrollmentFactory.Create(
            cmd.StudentId, cmd.SubjectId,
            studentInfo.FullName, subject.Name, subject.Code,
            cmd.Notes);

        // 6. Persistir y actualizar contador de la asignatura
        subject.IncrementEnrollments();
        await _enrollRepo.AddAsync(enrollment);
        await _subjectRepo.UpdateAsync(subject);

        // 7. PATRÓN OBSERVER: publicar evento de dominio
        await _events.PublishAsync(new EnrollmentCreatedEvent(
            enrollment.Id, enrollment.StudentId, enrollment.SubjectId,
            studentInfo.FullName, subject.Name, subject.Code));

        return ToDto(enrollment);
    }

    internal static EnrollmentDto ToDto(global::Enrollments.Domain.Entities.Enrollment e) => new(
        e.Id, e.StudentId, e.StudentName, e.SubjectId,
        e.SubjectName, e.SubjectCode, e.Status, e.Notes,
        e.EnrolledAt, e.CancelledAt, e.UpdatedAt);
}
