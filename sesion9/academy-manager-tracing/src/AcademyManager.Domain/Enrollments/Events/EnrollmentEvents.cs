using AcademyManager.Domain.Common;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;

namespace AcademyManager.Domain.Enrollments.Events;

public sealed record EnrollmentCreatedEvent(
    EnrollmentId EnrollmentId,
    StudentId StudentId,
    SubjectId SubjectId,
    DateTime EnrolledAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record EnrollmentCompletedEvent(
    EnrollmentId EnrollmentId,
    StudentId StudentId,
    SubjectId SubjectId,
    DateTime CompletedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record EnrollmentCancelledEvent(
    EnrollmentId EnrollmentId,
    StudentId StudentId,
    SubjectId SubjectId,
    DateTime CancelledAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
