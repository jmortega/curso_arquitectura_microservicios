using Dapper;
using Enrollments.Domain.Entities;
using Enrollments.Domain.Interfaces;
using MySql.Data.MySqlClient;

namespace Enrollments.Infrastructure.Persistence.Repositories;

public sealed class EnrollmentRepository : IEnrollmentRepository
{
    private readonly string _connectionString;

    public EnrollmentRepository(string connectionString)
        => _connectionString = connectionString;

    private MySqlConnection Open() => new(_connectionString);

    // Query base con JOIN para obtener nombres desnormalizados
    private const string BaseSelect = @"
        SELECT
            e.id, e.student_id, e.subject_id, e.status, e.notes,
            e.enrolled_at, e.cancelled_at, e.updated_at,
            e.student_name, e.subject_name, e.subject_code
        FROM enrollments e";

    public async Task<IEnumerable<Enrollment>> GetAllAsync()
    {
        using var cn = Open();
        var rows = await cn.QueryAsync($"{BaseSelect} ORDER BY e.enrolled_at DESC");
        return rows.Select(Map);
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id)
    {
        using var cn = Open();
        var row = await cn.QueryFirstOrDefaultAsync(
            $"{BaseSelect} WHERE e.id = @Id",
            new { Id = id.ToString() });
        return row is null ? null : Map(row);
    }

    public async Task<IEnumerable<Enrollment>> GetByStudentIdAsync(Guid studentId)
    {
        using var cn = Open();
        var rows = await cn.QueryAsync(
            $"{BaseSelect} WHERE e.student_id = @StudentId ORDER BY e.enrolled_at DESC",
            new { StudentId = studentId.ToString() });
        return rows.Select(Map);
    }

    public async Task<IEnumerable<Enrollment>> GetBySubjectIdAsync(Guid subjectId)
    {
        using var cn = Open();
        var rows = await cn.QueryAsync(
            $"{BaseSelect} WHERE e.subject_id = @SubjectId ORDER BY e.enrolled_at DESC",
            new { SubjectId = subjectId.ToString() });
        return rows.Select(Map);
    }

    public async Task<Enrollment?> GetActiveByStudentAndSubjectAsync(Guid studentId, Guid subjectId)
    {
        using var cn = Open();
        var row = await cn.QueryFirstOrDefaultAsync(
            $"{BaseSelect} WHERE e.student_id = @StudentId AND e.subject_id = @SubjectId AND e.status = 'Active'",
            new { StudentId = studentId.ToString(), SubjectId = subjectId.ToString() });
        return row is null ? null : Map(row);
    }

    public async Task<IEnumerable<Enrollment>> GetActiveByStudentIdAsync(Guid studentId)
    {
        using var cn = Open();
        var rows = await cn.QueryAsync(
            $"{BaseSelect} WHERE e.student_id = @StudentId AND e.status = 'Active'",
            new { StudentId = studentId.ToString() });
        return rows.Select(Map);
    }

    public async Task<Guid> AddAsync(Enrollment enrollment)
    {
        const string sql = @"
            INSERT INTO enrollments
                (id, student_id, subject_id, status, notes,
                 enrolled_at, cancelled_at, updated_at,
                 student_name, subject_name, subject_code)
            VALUES
                (@Id, @StudentId, @SubjectId, @Status, @Notes,
                 @EnrolledAt, @CancelledAt, @UpdatedAt,
                 @StudentName, @SubjectName, @SubjectCode)";

        using var cn = Open();
        await cn.ExecuteAsync(sql, new
        {
            Id          = enrollment.Id.ToString(),
            StudentId   = enrollment.StudentId.ToString(),
            SubjectId   = enrollment.SubjectId.ToString(),
            enrollment.Status,
            enrollment.Notes,
            enrollment.EnrolledAt,
            enrollment.CancelledAt,
            enrollment.UpdatedAt,
            enrollment.StudentName,
            enrollment.SubjectName,
            enrollment.SubjectCode,
        });
        return enrollment.Id;
    }

    public async Task<bool> UpdateAsync(Enrollment enrollment)
    {
        const string sql = @"
            UPDATE enrollments SET
                status       = @Status,
                notes        = @Notes,
                cancelled_at = @CancelledAt,
                updated_at   = @UpdatedAt
            WHERE id = @Id";

        using var cn = Open();
        var rows = await cn.ExecuteAsync(sql, new
        {
            enrollment.Status,
            enrollment.Notes,
            enrollment.CancelledAt,
            enrollment.UpdatedAt,
            Id = enrollment.Id.ToString(),
        });
        return rows > 0;
    }

    private static Enrollment Map(dynamic r) => Enrollment.Reconstitute(
        id:          Guid.Parse(r.id.ToString()),
        studentId:   Guid.Parse(r.student_id.ToString()),
        subjectId:   Guid.Parse(r.subject_id.ToString()),
        status:      (string)r.status,
        notes:       r.notes        is DBNull ? null : (string?)r.notes,
        enrolledAt:  (DateTime)r.enrolled_at,
        cancelledAt: r.cancelled_at is DBNull ? null : (DateTime?)r.cancelled_at,
        updatedAt:   (DateTime)r.updated_at,
        studentName: r.student_name  is DBNull ? null : (string?)r.student_name,
        subjectName: r.subject_name  is DBNull ? null : (string?)r.subject_name,
        subjectCode: r.subject_code  is DBNull ? null : (string?)r.subject_code);
}
