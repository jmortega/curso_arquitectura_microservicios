using AcademyManager.Domain.Common;
using AcademyManager.Domain.Subjects.Events;

namespace AcademyManager.Domain.Subjects;

public record SubjectId(Guid Value)
{
    public static SubjectId Create() => new(Guid.NewGuid());
    public static SubjectId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

public sealed class Subject : Entity<SubjectId>
{
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public int Credits { get; private set; }
    public int MaxStudents { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private Subject() { }

    private Subject(SubjectId id, string name, string code, string description, int credits, int maxStudents)
        : base(id)
    {
        Name = name;
        Code = code;
        Description = description;
        Credits = credits;
        MaxStudents = maxStudents;
        CreatedAt = DateTime.UtcNow;
    }

    public static Subject Create(string name, string code, string description, int credits, int maxStudents)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (credits <= 0) throw new ArgumentException("Credits must be positive.", nameof(credits));
        if (maxStudents <= 0) throw new ArgumentException("MaxStudents must be positive.", nameof(maxStudents));

        var id = SubjectId.Create();
        var subject = new Subject(id, name.Trim(), code.Trim().ToUpper(), description.Trim(), credits, maxStudents);

        subject.AddDomainEvent(new SubjectCreatedEvent(
            id, name, code, description, credits, maxStudents, subject.CreatedAt));

        return subject;
    }

    public void Update(string name, string description, int credits, int maxStudents)
    {
        Name = name.Trim();
        Description = description.Trim();
        Credits = credits;
        MaxStudents = maxStudents;
        AddDomainEvent(new SubjectUpdatedEvent(Id, name, description, credits, maxStudents));
    }
}
