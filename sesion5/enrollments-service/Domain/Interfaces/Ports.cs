using Enrollments.Domain.Entities;
using Enrollments.Domain.Events;

namespace Enrollments.Domain.Interfaces;

// ════════════════════════════════════════════════════════════════════════════
// REPOSITORIOS — Puerto de salida (hexagonal)
// PATRÓN REPOSITORY: abstrae el acceso a datos. El dominio no conoce MySQL.
// ════════════════════════════════════════════════════════════════════════════

public interface ISubjectRepository
{
    Task<IEnumerable<Subject>> GetAllAsync(bool onlyActive = true);
    Task<Subject?>             GetByIdAsync(Guid id);
    Task<Subject?>             GetByCodeAsync(string code);
    Task<Guid>                 AddAsync(Subject subject);
    Task<bool>                 UpdateAsync(Subject subject);
    Task<bool>                 DeleteAsync(Guid id);
}

public interface IEnrollmentRepository
{
    Task<IEnumerable<Enrollment>> GetAllAsync();
    Task<Enrollment?>             GetByIdAsync(Guid id);
    Task<IEnumerable<Enrollment>> GetByStudentIdAsync(Guid studentId);
    Task<IEnumerable<Enrollment>> GetBySubjectIdAsync(Guid subjectId);
    Task<Enrollment?>             GetActiveByStudentAndSubjectAsync(Guid studentId, Guid subjectId);
    Task<IEnumerable<Enrollment>> GetActiveByStudentIdAsync(Guid studentId);
    Task<Guid>                    AddAsync(Enrollment enrollment);
    Task<bool>                    UpdateAsync(Enrollment enrollment);
}

// ════════════════════════════════════════════════════════════════════════════
// PUBLICADOR DE EVENTOS — Puerto de salida (hexagonal)
// PATRÓN OBSERVER: IEventPublisher es el Subject (publicador).
// Las implementaciones (RabbitMQ, in-memory) son los Concrete Subjects.
// ════════════════════════════════════════════════════════════════════════════

public interface IEventPublisher
{
    Task PublishAsync<T>(T domainEvent) where T : IDomainEvent;
}

// ════════════════════════════════════════════════════════════════════════════
// CLIENTE DEL SERVICIO DE ESTUDIANTES — Puerto de salida (hexagonal)
// Permite consultar si un alumno existe y está activo sin acoplamiento.
// ════════════════════════════════════════════════════════════════════════════

public interface IStudentServiceClient
{
    Task<StudentInfo?> GetStudentAsync(Guid studentId);
}

public record StudentInfo(
    Guid   Id,
    string FullName,
    string Email,
    string EnrollmentNumber,
    bool   IsActive
);

// ════════════════════════════════════════════════════════════════════════════
// ESTRATEGIA DE VALIDACIÓN — Puerto interno
// PATRÓN STRATEGY: permite intercambiar reglas de validación en runtime.
// ════════════════════════════════════════════════════════════════════════════

public interface IEnrollmentValidationStrategy
{
    string Name { get; }
    Task<ValidationResult> ValidateAsync(EnrollmentValidationContext context);
}

public record EnrollmentValidationContext(
    Guid        StudentId,
    Subject     Subject,
    StudentInfo StudentInfo
);

public record ValidationResult(bool IsValid, string? ErrorMessage)
{
    public static ValidationResult Ok()    => new(true,  null);
    public static ValidationResult Fail(string msg) => new(false, msg);
}
