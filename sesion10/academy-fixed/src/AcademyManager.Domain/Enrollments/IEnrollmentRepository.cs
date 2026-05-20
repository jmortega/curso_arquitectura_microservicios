using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;

namespace AcademyManager.Domain.Enrollments;

/// <summary>
/// Write-side port for Enrollment persistence.
/// </summary>
public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(EnrollmentId id, CancellationToken ct = default);
    Task<bool> ExistsAsync(StudentId studentId, SubjectId subjectId, CancellationToken ct = default);
    Task<int> CountActiveBySubjectAsync(SubjectId subjectId, CancellationToken ct = default);
    Task AddAsync(Enrollment enrollment, CancellationToken ct = default);
    void Update(Enrollment enrollment);
    Task SaveChangesAsync(CancellationToken ct = default);
}
