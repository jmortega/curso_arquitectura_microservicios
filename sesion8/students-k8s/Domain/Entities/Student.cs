namespace Students.Domain.Entities;

public class Student
{
    public Guid      Id               { get; private set; }
    public string    FirstName        { get; private set; }
    public string    LastName         { get; private set; }
    public string    Email            { get; private set; }
    public string    EnrollmentNumber { get; private set; }
    public DateOnly? DateOfBirth      { get; private set; }
    public string?   Phone            { get; private set; }
    public string?   Address          { get; private set; }
    public bool      IsActive         { get; private set; }
    public DateTime  CreatedAt        { get; private set; }
    public DateTime  UpdatedAt        { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    // Requerido por Dapper
    private Student()
    {
        FirstName        = string.Empty;
        LastName         = string.Empty;
        Email            = string.Empty;
        EnrollmentNumber = string.Empty;
    }

    /// <summary>Factory: única forma de crear un alumno nuevo.</summary>
    public static Student Create(
        string   firstName,
        string   lastName,
        string   email,
        string   enrollmentNumber,
        DateOnly? dateOfBirth = null,
        string?  phone   = null,
        string?  address = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))        throw new ArgumentException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(lastName))         throw new ArgumentException("El apellido es obligatorio.");
        if (string.IsNullOrWhiteSpace(email))            throw new ArgumentException("El email es obligatorio.");
        if (string.IsNullOrWhiteSpace(enrollmentNumber)) throw new ArgumentException("La matrícula es obligatoria.");

        return new Student
        {
            Id               = Guid.NewGuid(),
            FirstName        = firstName.Trim(),
            LastName         = lastName.Trim(),
            Email            = email.Trim().ToLowerInvariant(),
            EnrollmentNumber = enrollmentNumber.Trim().ToUpperInvariant(),
            DateOfBirth      = dateOfBirth,
            Phone            = phone?.Trim(),
            Address          = address?.Trim(),
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };
    }

    public void Update(
        string firstName, string lastName, string email,
        DateOnly? dateOfBirth, string? phone, string? address)
    {
        FirstName   = firstName.Trim();
        LastName    = lastName.Trim();
        Email       = email.Trim().ToLowerInvariant();
        DateOfBirth = dateOfBirth;
        Phone       = phone?.Trim();
        Address     = address?.Trim();
        UpdatedAt   = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;

    /// <summary>Reconstitución desde base de datos (usado por el repositorio).</summary>
    public static Student Reconstitute(
        Guid id, string firstName, string lastName, string email,
        string enrollmentNumber, DateOnly? dateOfBirth, string? phone,
        string? address, bool isActive, DateTime createdAt, DateTime updatedAt)
        => new()
        {
            Id               = id,
            FirstName        = firstName,
            LastName         = lastName,
            Email            = email,
            EnrollmentNumber = enrollmentNumber,
            DateOfBirth      = dateOfBirth,
            Phone            = phone,
            Address          = address,
            IsActive         = isActive,
            CreatedAt        = createdAt,
            UpdatedAt        = updatedAt
        };
}