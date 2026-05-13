namespace AcademyManager.Domain.Students;

/// <summary>
/// Write-side port: defines how the domain persists Student aggregates.
/// Implemented in the Infrastructure layer (adapter).
/// </summary>
public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(StudentId id, CancellationToken ct = default);
    Task<Student?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(Student student, CancellationToken ct = default);
    void Update(Student student);
    void Remove(Student student);
    Task<bool> ExistsAsync(StudentId id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
