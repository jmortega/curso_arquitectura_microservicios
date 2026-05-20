using AcademyManager.Domain.Common;

namespace AcademyManager.Domain.Students.Events;

public sealed record StudentCreatedEvent(
    StudentId StudentId,
    string FirstName,
    string LastName,
    string Email,
    DateTime DateOfBirth,
    DateTime CreatedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record StudentUpdatedEvent(
    StudentId StudentId,
    string FirstName,
    string LastName,
    string Email,
    DateTime UpdatedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public sealed record StudentDeletedEvent(
    StudentId StudentId,
    DateTime DeletedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
