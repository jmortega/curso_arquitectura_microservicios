using Enrollments.Application.Commands.EnrollStudent;
using Enrollments.Application.DTOs;
using Enrollments.Domain.Events;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.Interfaces;
using MediatR;

namespace Enrollments.Application.Commands.CancelEnrollment;

public record CancelEnrollmentCommand(Guid EnrollmentId, string? Reason) : IRequest<EnrollmentDto>;

public sealed class CancelEnrollmentCommandHandler : IRequestHandler<CancelEnrollmentCommand, EnrollmentDto>
{
    private readonly IEnrollmentRepository _enrollRepo;
    private readonly ISubjectRepository    _subjectRepo;
    private readonly IEventPublisher       _events;

    public CancelEnrollmentCommandHandler(
        IEnrollmentRepository enrollRepo,
        ISubjectRepository    subjectRepo,
        IEventPublisher       events)
    {
        _enrollRepo  = enrollRepo;
        _subjectRepo = subjectRepo;
        _events      = events;
    }

    public async Task<EnrollmentDto> Handle(CancelEnrollmentCommand cmd, CancellationToken ct)
    {
        var enrollment = await _enrollRepo.GetByIdAsync(cmd.EnrollmentId)
            ?? throw new EnrollmentNotFoundException(cmd.EnrollmentId);

        enrollment.Cancel(cmd.Reason);
        await _enrollRepo.UpdateAsync(enrollment);

        // Liberar plaza en la asignatura
        var subject = await _subjectRepo.GetByIdAsync(enrollment.SubjectId);
        if (subject is not null)
        {
            subject.DecrementEnrollments();
            await _subjectRepo.UpdateAsync(subject);
        }

        // OBSERVER: notificar cancelación
        await _events.PublishAsync(new EnrollmentCancelledEvent(
            enrollment.Id, enrollment.StudentId, enrollment.SubjectId,
            cmd.Reason ?? "Sin motivo especificado"));

        return EnrollStudentCommandHandler.ToDto(enrollment);
    }
}
