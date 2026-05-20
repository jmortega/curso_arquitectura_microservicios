using AcademyManager.Domain.Common;
using AcademyManager.Domain.Students.Events;

namespace AcademyManager.Domain.Students;

public sealed class Student : Entity<StudentId>
{
    public StudentName Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public DateTime DateOfBirth { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private Student() { }

    private Student(StudentId id, StudentName name, Email email, DateTime dateOfBirth)
        : base(id)
    {
        Name = name;
        Email = email;
        DateOfBirth = dateOfBirth;
        CreatedAt = DateTime.UtcNow;
    }

    public static Student Create(string firstName, string lastName, string email, DateTime dateOfBirth)
    {
        var id = StudentId.Create();
        var student = new Student(
            id,
            StudentName.Create(firstName, lastName),
            Email.Create(email),
            dateOfBirth);

        student.AddDomainEvent(new StudentCreatedEvent(
            id, firstName, lastName, email, dateOfBirth, student.CreatedAt));

        return student;
    }

    public void Update(string firstName, string lastName, string email)
    {
        Name = StudentName.Create(firstName, lastName);
        Email = Email.Create(email);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new StudentUpdatedEvent(Id, firstName, lastName, email, UpdatedAt.Value));
    }

    public void MarkDeleted()
    {
        AddDomainEvent(new StudentDeletedEvent(Id, DateTime.UtcNow));
    }
}
