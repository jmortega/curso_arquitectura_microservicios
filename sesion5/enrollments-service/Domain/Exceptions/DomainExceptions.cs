namespace Enrollments.Domain.Exceptions;

public sealed class SubjectNotFoundException : Exception
{
    public SubjectNotFoundException(Guid id)
        : base($"Asignatura con ID '{id}' no encontrada.") { }

    public SubjectNotFoundException(string code)
        : base($"Asignatura con código '{code}' no encontrada.") { }
}

public sealed class SubjectCodeAlreadyExistsException : Exception
{
    public SubjectCodeAlreadyExistsException(string code)
        : base($"Ya existe una asignatura con el código '{code}'.") { }
}

public sealed class EnrollmentNotFoundException : Exception
{
    public EnrollmentNotFoundException(Guid id)
        : base($"Matrícula con ID '{id}' no encontrada.") { }
}

public sealed class DuplicateEnrollmentException : Exception
{
    public DuplicateEnrollmentException(Guid studentId, Guid subjectId)
        : base($"El alumno '{studentId}' ya tiene una matrícula activa en la asignatura '{subjectId}'.") { }
}

public sealed class StudentNotFoundException : Exception
{
    public StudentNotFoundException(Guid studentId)
        : base($"El alumno con ID '{studentId}' no existe en el sistema de estudiantes.") { }
}

public sealed class EnrollmentValidationException : Exception
{
    public EnrollmentValidationException(string strategy, string message)
        : base($"[{strategy}] {message}") { }
}
