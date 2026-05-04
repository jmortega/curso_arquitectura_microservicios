namespace Enrollments.Domain.Events;

// ── Interfaz base para todos los eventos de dominio ──────────────────────────
// PATRÓN OBSERVER: los eventos representan "algo que ocurrió" en el dominio.
// Los publicadores no conocen a los suscriptores (desacoplamiento).

public interface IDomainEvent
{
    Guid     EventId   { get; }
    DateTime OccurredAt { get; }
    string   EventType  { get; }
}

// ── Eventos publicados por el servicio de matrículas ─────────────────────────

/// <summary>Un alumno se matriculó en una asignatura.</summary>
public record EnrollmentCreatedEvent(
    Guid     EnrollmentId,
    Guid     StudentId,
    Guid     SubjectId,
    string   StudentName,
    string   SubjectName,
    string   SubjectCode
) : IDomainEvent
{
    public Guid     EventId    { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string   EventType  => "enrollment.created";
}

/// <summary>Una matrícula fue cancelada.</summary>
public record EnrollmentCancelledEvent(
    Guid    EnrollmentId,
    Guid    StudentId,
    Guid    SubjectId,
    string  Reason
) : IDomainEvent
{
    public Guid     EventId    { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string   EventType  => "enrollment.cancelled";
}

/// <summary>Una asignatura fue creada.</summary>
public record SubjectCreatedEvent(
    Guid   SubjectId,
    string Code,
    string Name,
    int    Credits,
    int    MaxCapacity
) : IDomainEvent
{
    public Guid     EventId    { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string   EventType  => "subject.created";
}

// ── Eventos CONSUMIDOS desde el servicio de estudiantes ───────────────────────

/// <summary>
/// Evento externo publicado por el servicio de estudiantes cuando
/// un alumno es desactivado. Este servicio debe cancelar sus matrículas activas.
/// </summary>
public record StudentDeactivatedEvent(
    Guid   StudentId,
    string StudentName,
    string Email
)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string   EventType  => "student.deactivated";
}

/// <summary>Evento externo: alumno creado en el servicio de estudiantes.</summary>
public record StudentCreatedEvent(
    Guid   StudentId,
    string FirstName,
    string LastName,
    string Email,
    string EnrollmentNumber
)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string   EventType  => "student.created";
}
