using AcademyManager.Domain.Common;

namespace AcademyManager.Domain.Students;

public record StudentId(Guid Value)
{
    public static StudentId Create() => new(Guid.NewGuid());
    public static StudentId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

public sealed class StudentName : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => $"{FirstName} {LastName}";

    private StudentName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static StudentName Create(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));
        return new StudentName(firstName.Trim(), lastName.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }
}

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        if (!email.Contains('@'))
            throw new ArgumentException("Email format is invalid.", nameof(email));
        return new Email(email.Trim().ToLowerInvariant());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
