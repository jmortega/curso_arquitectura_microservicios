using Enrollments.Domain.Entities;

namespace Enrollments.Domain.Factories;

// ════════════════════════════════════════════════════════════════════════════
// PATRÓN FACTORY
// Centraliza la lógica de construcción de entidades complejas.
// El controlador/handler nunca usa `new Subject()` directamente.
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Factory para crear entidades Subject validando las invariantes de dominio.
/// </summary>
public static class SubjectFactory
{
    public static Subject Create(
        string  code,
        string  name,
        string? description,
        int     credits,
        int     maxCapacity)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("El código de la asignatura es obligatorio.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la asignatura es obligatorio.");
        if (credits is < 1 or > 12)
            throw new ArgumentException("Los créditos deben estar entre 1 y 12.");
        if (maxCapacity < 1)
            throw new ArgumentException("La capacidad máxima debe ser al menos 1.");

        return Subject.Reconstitute(
            id:                 Guid.NewGuid(),
            code:               code.Trim().ToUpperInvariant(),
            name:               name.Trim(),
            description:        description?.Trim(),
            credits:            credits,
            maxCapacity:        maxCapacity,
            currentEnrollments: 0,
            isActive:           true,
            createdAt:          DateTime.UtcNow,
            updatedAt:          DateTime.UtcNow);
    }
}

/// <summary>
/// Factory para crear entidades Enrollment validando las invariantes de dominio.
/// </summary>
public static class EnrollmentFactory
{
    public static Enrollment Create(
        Guid    studentId,
        Guid    subjectId,
        string  studentName,
        string  subjectName,
        string  subjectCode,
        string? notes = null)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("El ID del alumno es inválido.");
        if (subjectId == Guid.Empty)
            throw new ArgumentException("El ID de la asignatura es inválido.");

        return Enrollment.Reconstitute(
            id:          Guid.NewGuid(),
            studentId:   studentId,
            subjectId:   subjectId,
            status:      "Active",
            notes:       notes,
            enrolledAt:  DateTime.UtcNow,
            cancelledAt: null,
            updatedAt:   DateTime.UtcNow,
            studentName: studentName,
            subjectName: subjectName,
            subjectCode: subjectCode);
    }
}
