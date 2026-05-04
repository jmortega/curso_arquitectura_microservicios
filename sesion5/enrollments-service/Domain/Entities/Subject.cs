namespace Enrollments.Domain.Entities;

/// <summary>
/// Entidad de dominio Asignatura.
/// Encapsula las reglas de negocio relativas a una materia académica.
/// </summary>
public sealed class Subject
{
    public Guid     Id          { get; private set; }
    public string   Code        { get; private set; }   // ej. "MAT-101"
    public string   Name        { get; private set; }
    public string?  Description { get; private set; }
    public int      Credits     { get; private set; }
    public int      MaxCapacity { get; private set; }
    public int      CurrentEnrollments { get; private set; }
    public bool     IsActive    { get; private set; }
    public DateTime CreatedAt   { get; private set; }
    public DateTime UpdatedAt   { get; private set; }

    public bool HasAvailableSlots => CurrentEnrollments < MaxCapacity;
    public int  AvailableSlots   => MaxCapacity - CurrentEnrollments;

    private Subject()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    // ── Reconstitución desde DB (usada por el repositorio) ───────────────────
    public static Subject Reconstitute(
        Guid id, string code, string name, string? description,
        int credits, int maxCapacity, int currentEnrollments,
        bool isActive, DateTime createdAt, DateTime updatedAt)
        => new()
        {
            Id = id, Code = code, Name = name, Description = description,
            Credits = credits, MaxCapacity = maxCapacity,
            CurrentEnrollments = currentEnrollments,
            IsActive = isActive, CreatedAt = createdAt, UpdatedAt = updatedAt
        };

    public void Update(string name, string? description, int credits, int maxCapacity)
    {
        Name        = name.Trim();
        Description = description?.Trim();
        Credits     = credits;
        MaxCapacity = maxCapacity;
        UpdatedAt   = DateTime.UtcNow;
    }

    public void IncrementEnrollments()
    {
        if (!HasAvailableSlots)
            throw new InvalidOperationException($"La asignatura '{Name}' no tiene plazas disponibles.");
        CurrentEnrollments++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementEnrollments()
    {
        if (CurrentEnrollments > 0) CurrentEnrollments--;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    public void Activate()   { IsActive = true;  UpdatedAt = DateTime.UtcNow; }
}
