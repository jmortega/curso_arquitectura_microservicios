namespace AcademyManager.Domain.Subjects;

/// <summary>
/// Write-side port for Subject persistence.
/// </summary>
public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(SubjectId id, CancellationToken ct = default);
    Task<Subject?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<Subject>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Subject subject, CancellationToken ct = default);
    void Update(Subject subject);
    Task<bool> ExistsAsync(SubjectId id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
