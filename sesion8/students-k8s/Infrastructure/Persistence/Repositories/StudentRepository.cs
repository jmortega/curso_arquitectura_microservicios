using System.Data;
using Dapper;
using MySql.Data.MySqlClient;
using Students.Domain.Entities;
using Students.Domain.Interfaces;

namespace Students.Infrastructure.Persistence.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly string _connectionString;

    public StudentRepository(string connectionString) => _connectionString = connectionString;

    private IDbConnection OpenConnection() => new MySqlConnection(_connectionString);

    // Query base con aliases explícitos — Dapper mapea por nombre de columna/alias
    private const string SelectColumns = @"
        SELECT
            CAST(id AS CHAR)                        AS Id,
            first_name                              AS FirstName,
            last_name                               AS LastName,
            email                                   AS Email,
            enrollment_number                       AS EnrollmentNumber,
            DATE_FORMAT(date_of_birth, '%Y-%m-%d')  AS DateOfBirth,
            phone                                   AS Phone,
            address                                 AS Address,
            is_active                               AS IsActive,
            created_at                              AS CreatedAt,
            updated_at                              AS UpdatedAt
        FROM students";

    // ── Queries ──────────────────────────────────────────────
    public async Task<IEnumerable<Student>> GetAllAsync(bool onlyActive = true)
    {
        var sql = onlyActive
            ? $"{SelectColumns} WHERE is_active = 1 ORDER BY last_name, first_name"
            : $"{SelectColumns} ORDER BY last_name, first_name";

        using var conn = OpenConnection();
        var rows = await conn.QueryAsync<StudentRow>(sql);
        return rows.Select(ToDomain);
    }

    public async Task<Student?> GetByIdAsync(Guid id)
    {
        var sql = $"{SelectColumns} WHERE id = @Id LIMIT 1";
        using var conn = OpenConnection();
        var row = await conn.QuerySingleOrDefaultAsync<StudentRow>(sql, new { Id = id.ToString() });
        return row is null ? null : ToDomain(row);
    }

    public async Task<Student?> GetByEmailAsync(string email)
    {
        var sql = $"{SelectColumns} WHERE email = @Email LIMIT 1";
        using var conn = OpenConnection();
        var row = await conn.QuerySingleOrDefaultAsync<StudentRow>(sql, new { Email = email.ToLowerInvariant() });
        return row is null ? null : ToDomain(row);
    }

    public async Task<Student?> GetByEnrollmentAsync(string enrollmentNumber)
    {
        var sql = $"{SelectColumns} WHERE enrollment_number = @Number LIMIT 1";
        using var conn = OpenConnection();
        var row = await conn.QuerySingleOrDefaultAsync<StudentRow>(sql, new { Number = enrollmentNumber });
        return row is null ? null : ToDomain(row);
    }

    // ── Commands ─────────────────────────────────────────────
    public async Task<Guid> AddAsync(Student s)
    {
        const string sql = @"
            INSERT INTO students
                (id, first_name, last_name, email, enrollment_number,
                 date_of_birth, phone, address, is_active, created_at, updated_at)
            VALUES
                (@Id, @FirstName, @LastName, @Email, @EnrollmentNumber,
                 @DateOfBirth, @Phone, @Address, @IsActive, @CreatedAt, @UpdatedAt)";

        using var conn = OpenConnection();
        await conn.ExecuteAsync(sql, new
        {
            Id               = s.Id.ToString(),
            s.FirstName, s.LastName, s.Email, s.EnrollmentNumber,
            DateOfBirth      = s.DateOfBirth?.ToString("yyyy-MM-dd"),
            s.Phone, s.Address,
            IsActive         = s.IsActive ? 1 : 0,
            s.CreatedAt, s.UpdatedAt
        });
        return s.Id;
    }

    public async Task<bool> UpdateAsync(Student s)
    {
        const string sql = @"
            UPDATE students SET
                first_name    = @FirstName,
                last_name     = @LastName,
                email         = @Email,
                date_of_birth = @DateOfBirth,
                phone         = @Phone,
                address       = @Address,
                updated_at    = @UpdatedAt
            WHERE id = @Id";

        using var conn = OpenConnection();
        return await conn.ExecuteAsync(sql, new
        {
            Id          = s.Id.ToString(),
            s.FirstName, s.LastName, s.Email,
            DateOfBirth = s.DateOfBirth?.ToString("yyyy-MM-dd"),
            s.Phone, s.Address, s.UpdatedAt
        }) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = "UPDATE students SET is_active = 0, updated_at = @Now WHERE id = @Id";
        using var conn = OpenConnection();
        return await conn.ExecuteAsync(sql, new { Id = id.ToString(), Now = DateTime.UtcNow }) > 0;
    }

    // ── Internal row / mapping ────────────────────────────────
    private sealed class StudentRow
    {
        public string   Id               { get; set; } = "";
        public string   FirstName        { get; set; } = "";
        public string   LastName         { get; set; } = "";
        public string   Email            { get; set; } = "";
        public string   EnrollmentNumber { get; set; } = "";
        public string?  DateOfBirth      { get; set; }
        public string?  Phone            { get; set; }
        public string?  Address          { get; set; }
        public bool     IsActive         { get; set; }
        public DateTime CreatedAt        { get; set; }
        public DateTime UpdatedAt        { get; set; }
    }

    private static Student ToDomain(StudentRow r) => Student.Reconstitute(
        Guid.Parse(r.Id),
        r.FirstName, r.LastName, r.Email, r.EnrollmentNumber,
        r.DateOfBirth is null ? null : DateOnly.Parse(r.DateOfBirth),
        r.Phone, r.Address, r.IsActive, r.CreatedAt, r.UpdatedAt);
}