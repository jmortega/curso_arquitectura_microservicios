using AcademyManager.Domain.Common;

namespace AcademyManager.Domain.Subjects.Events;

public sealed record SubjectCreatedEvent(
    SubjectId SubjectId,
    string Name,
    string Code,
    string Description,
    int Credits,
    int MaxStudents,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record SubjectUpdatedEvent(
    SubjectId SubjectId,
    string Name,
    string Description,
    int Credits,
    int MaxStudents) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
