using AcademyManager.Domain.Enrollments;
using AcademyManager.Domain.Students;
using AcademyManager.Domain.Subjects;
using Microsoft.EntityFrameworkCore;

namespace AcademyManager.Infrastructure.Persistence.Write.Repositories
{
    internal sealed class StudentRepository : IStudentRepository
    {
        private readonly WriteDbContext _context;

        public StudentRepository(WriteDbContext context) => _context = context;

        public async Task<Student?> GetByIdAsync(StudentId id, CancellationToken ct) =>
            await _context.Students.FirstOrDefaultAsync(s => s.Id == id, ct);

        public async Task<Student?> GetByEmailAsync(string email, CancellationToken ct) =>
            await _context.Students
                .FirstOrDefaultAsync(s => s.Email.Value == email.ToLowerInvariant(), ct);

        public async Task AddAsync(Student student, CancellationToken ct) =>
            await _context.Students.AddAsync(student, ct);

        public void Update(Student student) => _context.Students.Update(student);

        public void Remove(Student student) => _context.Students.Remove(student);

        public async Task<bool> ExistsAsync(StudentId id, CancellationToken ct) =>
            await _context.Students.AnyAsync(s => s.Id == id, ct);

        public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
    }

    internal sealed class SubjectRepository : ISubjectRepository
    {
        private readonly WriteDbContext _context;

        public SubjectRepository(WriteDbContext context) => _context = context;

        public async Task<Subject?> GetByIdAsync(SubjectId id, CancellationToken ct) =>
            await _context.Subjects.FirstOrDefaultAsync(s => s.Id == id, ct);

        public async Task<Subject?> GetByCodeAsync(string code, CancellationToken ct) =>
            await _context.Subjects
                .FirstOrDefaultAsync(s => s.Code == code.ToUpper(), ct);

        public async Task<IEnumerable<Subject>> GetAllAsync(CancellationToken ct) =>
            await _context.Subjects.ToListAsync(ct);

        public async Task AddAsync(Subject subject, CancellationToken ct) =>
            await _context.Subjects.AddAsync(subject, ct);

        public void Update(Subject subject) => _context.Subjects.Update(subject);

        public async Task<bool> ExistsAsync(SubjectId id, CancellationToken ct) =>
            await _context.Subjects.AnyAsync(s => s.Id == id, ct);

        public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
    }

    internal sealed class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly WriteDbContext _context;

        public EnrollmentRepository(WriteDbContext context) => _context = context;

        public async Task<Enrollment?> GetByIdAsync(EnrollmentId id, CancellationToken ct) =>
            await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct);

        public async Task<bool> ExistsAsync(StudentId studentId, SubjectId subjectId, CancellationToken ct) =>
            await _context.Enrollments
                .AnyAsync(e => e.StudentId == studentId && e.SubjectId == subjectId
                               && e.Status == EnrollmentStatus.Active, ct);

        public async Task<int> CountActiveBySubjectAsync(SubjectId subjectId, CancellationToken ct) =>
            await _context.Enrollments
                .CountAsync(e => e.SubjectId == subjectId && e.Status == EnrollmentStatus.Active, ct);

        public async Task AddAsync(Enrollment enrollment, CancellationToken ct) =>
            await _context.Enrollments.AddAsync(enrollment, ct);

        public void Update(Enrollment enrollment) => _context.Enrollments.Update(enrollment);

        public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
    }
}
