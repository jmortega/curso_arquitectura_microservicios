using AcademyManager.Application.ReadModels;

namespace AcademyManager.Application.Common.Interfaces
{
    /// <summary>
    /// Read-side port: queries against the MongoDB read model.
    /// </summary>
    public interface IStudentReadRepository
    {
        Task<StudentReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<StudentReadModel>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
        Task<long> CountAsync(CancellationToken ct = default);
        Task UpsertAsync(StudentReadModel model, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
        Task AddEnrollmentSummaryAsync(Guid studentId, StudentReadModel.EnrollmentSummary summary, CancellationToken ct = default);
    }

    public interface ISubjectReadRepository
    {
        Task<SubjectReadModel?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<SubjectReadModel>> GetAllAsync(CancellationToken ct = default);
        Task UpsertAsync(SubjectReadModel model, CancellationToken ct = default);
        Task IncrementEnrolledStudentsAsync(Guid subjectId, int delta, CancellationToken ct = default);
    }

    public interface IEnrollmentReadRepository
    {
        Task<IEnumerable<EnrollmentReadModel>> GetByStudentIdAsync(Guid studentId, CancellationToken ct = default);
        Task<IEnumerable<EnrollmentReadModel>> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct = default);
        Task UpsertAsync(EnrollmentReadModel model, CancellationToken ct = default);
    }
}
