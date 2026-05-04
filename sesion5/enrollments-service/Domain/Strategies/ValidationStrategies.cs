using Enrollments.Domain.Interfaces;

namespace Enrollments.Domain.Strategies;

// ════════════════════════════════════════════════════════════════════════════
// PATRÓN STRATEGY
// Cada estrategia encapsula UNA regla de validación.
// Se pueden combinar dinámicamente sin modificar el código existente (OCP).
// El EnrollmentService actúa como Context, ejecutando la lista de estrategias.
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Estrategia 1: Verifica que la asignatura tenga plazas disponibles.
/// </summary>
public sealed class CapacityValidationStrategy : IEnrollmentValidationStrategy
{
    public string Name => "CapacityValidation";

    public Task<ValidationResult> ValidateAsync(EnrollmentValidationContext ctx)
    {
        if (!ctx.Subject.HasAvailableSlots)
            return Task.FromResult(ValidationResult.Fail(
                $"La asignatura '{ctx.Subject.Name}' no tiene plazas disponibles. " +
                $"Capacidad máxima: {ctx.Subject.MaxCapacity}, inscritos: {ctx.Subject.CurrentEnrollments}."));

        return Task.FromResult(ValidationResult.Ok());
    }
}

/// <summary>
/// Estrategia 2: Verifica que el alumno esté activo en el sistema.
/// </summary>
public sealed class ActiveStudentValidationStrategy : IEnrollmentValidationStrategy
{
    public string Name => "ActiveStudentValidation";

    public Task<ValidationResult> ValidateAsync(EnrollmentValidationContext ctx)
    {
        if (!ctx.StudentInfo.IsActive)
            return Task.FromResult(ValidationResult.Fail(
                $"El alumno '{ctx.StudentInfo.FullName}' no está activo y no puede matricularse."));

        return Task.FromResult(ValidationResult.Ok());
    }
}

/// <summary>
/// Estrategia 3: Verifica que la asignatura esté activa.
/// </summary>
public sealed class ActiveSubjectValidationStrategy : IEnrollmentValidationStrategy
{
    public string Name => "ActiveSubjectValidation";

    public Task<ValidationResult> ValidateAsync(EnrollmentValidationContext ctx)
    {
        if (!ctx.Subject.IsActive)
            return Task.FromResult(ValidationResult.Fail(
                $"La asignatura '{ctx.Subject.Name}' no está disponible para matrícula."));

        return Task.FromResult(ValidationResult.Ok());
    }
}

// ════════════════════════════════════════════════════════════════════════════
// PATRÓN FACTORY (combinado con Strategy)
// EnrollmentValidationStrategyFactory crea el conjunto de estrategias
// estándar, facilitando su inyección y extensibilidad.
// ════════════════════════════════════════════════════════════════════════════

public static class EnrollmentValidationStrategyFactory
{
    /// <summary>
    /// Devuelve el conjunto predeterminado de estrategias de validación
    /// para una matrícula estándar.
    /// </summary>
    public static IEnumerable<IEnrollmentValidationStrategy> CreateDefaultStrategies()
        =>
        [
            new ActiveStudentValidationStrategy(),
            new ActiveSubjectValidationStrategy(),
            new CapacityValidationStrategy(),
        ];
}
