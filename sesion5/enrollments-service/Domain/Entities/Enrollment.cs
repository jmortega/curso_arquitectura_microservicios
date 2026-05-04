namespace Enrollments.Domain.Entities;

/// <summary>
/// Entidad de dominio Matrícula.
/// Representa la inscripción de un alumno en una asignatura.
/// </summary>
public sealed class Enrollment
{
    public Guid     Id          { get; private set; }
    public Guid     StudentId   { get; private set; }
    public Guid     SubjectId   { get; private set; }
    public string   Status      { get; private set; }   // Active | Cancelled | Completed
    public string?  Notes       { get; private set; }
    public DateTime EnrolledAt  { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime UpdatedAt   { get; private set; }

    // Datos desnormalizados para consultas rápidas (sin JOIN)
    public string? StudentName    { get; private set; }
    public string? SubjectName    { get; private set; }
    public string? SubjectCode    { get; private set; }

    private Enrollment() { Status = string.Empty; }

    public static Enrollment Reconstitute(
        Guid id, Guid studentId, Guid subjectId, string status, string? notes,
        DateTime enrolledAt, DateTime? cancelledAt, DateTime updatedAt,
        string? studentName = null, string? subjectName = null, string? subjectCode = null)
        => new()
        {
            Id = id, StudentId = studentId, SubjectId = subjectId,
            Status = status, Notes = notes, EnrolledAt = enrolledAt,
            CancelledAt = cancelledAt, UpdatedAt = updatedAt,
            StudentName = studentName, SubjectName = subjectName, SubjectCode = subjectCode
        };

    public void Cancel(string? reason = null)
    {
        if (Status == "Cancelled")
            throw new InvalidOperationException("La matrícula ya está cancelada.");
        Status      = "Cancelled";
        Notes       = reason;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt   = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status    = "Completed";
        UpdatedAt = DateTime.UtcNow;
    }
}
