using AcademyManager.Domain.Common;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;
using AcademyManager.Domain.Enrollments.Events;

namespace AcademyManager.Domain.Enrollments;

public record EnrollmentId(Guid Value)
{
    public static EnrollmentId Create() => new(Guid.NewGuid());
    public static EnrollmentId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

public enum EnrollmentStatus { Active, Completed, Cancelled }

public sealed class Enrollment : Entity<EnrollmentId>
{
    public StudentId StudentId { get; private set; } = default!;
    public SubjectId SubjectId { get; private set; } = default!;
    public EnrollmentStatus Status { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // EF Core constructor
    private Enrollment() { }

    private Enrollment(EnrollmentId id, StudentId studentId, SubjectId subjectId)
        : base(id)
    {
        StudentId = studentId;
        SubjectId = subjectId;
        Status = EnrollmentStatus.Active;
        EnrolledAt = DateTime.UtcNow;
    }

    public static Enrollment Create(StudentId studentId, SubjectId subjectId)
    {
        var id = EnrollmentId.Create();
        var enrollment = new Enrollment(id, studentId, subjectId);

        enrollment.AddDomainEvent(new EnrollmentCreatedEvent(
            id, studentId, subjectId, enrollment.EnrolledAt));

        return enrollment;
    }

    public void Complete()
    {
        if (Status != EnrollmentStatus.Active)
            throw new InvalidOperationException("Only active enrollments can be completed.");
        Status = EnrollmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        AddDomainEvent(new EnrollmentCompletedEvent(Id, StudentId, SubjectId, CompletedAt.Value));
    }

    public void Cancel()
    {
        if (Status != EnrollmentStatus.Active)
            throw new InvalidOperationException("Only active enrollments can be cancelled.");
        Status = EnrollmentStatus.Cancelled;
        AddDomainEvent(new EnrollmentCancelledEvent(Id, StudentId, SubjectId, DateTime.UtcNow));
    }
}
