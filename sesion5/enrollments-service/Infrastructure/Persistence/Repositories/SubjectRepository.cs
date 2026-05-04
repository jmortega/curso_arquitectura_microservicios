using Dapper;
using Enrollments.Domain.Entities;
using Enrollments.Domain.Interfaces;
using MySql.Data.MySqlClient;

namespace Enrollments.Infrastructure.Persistence.Repositories;

/// <summary>
/// PATRÓN REPOSITORY — Adaptador de salida (hexagonal).
/// Implementa ISubjectRepository con Dapper + MySQL.
/// El dominio no sabe nada de MySQL.
/// </summary>
public sealed class SubjectRepository : ISubjectRepository
{
    private readonly string _connectionString;

    public SubjectRepository(string connectionString)
        => _connectionString = connectionString;

    private MySqlConnection Open() => new(_connectionString);

    public async Task<IEnumerable<Subject>> GetAllAsync(bool onlyActive = true)
    {
        var sql = onlyActive
            ? "SELECT * FROM subjects WHERE is_active = 1 ORDER BY code"
            : "SELECT * FROM subjects ORDER BY code";

        using var cn = Open();
        var rows = await cn.QueryAsync(sql);
        return rows.Select(Map);
    }

    public async Task<Subject?> GetByIdAsync(Guid id)
    {
        using var cn = Open();
        var row = await cn.QueryFirstOrDefaultAsync(
            "SELECT * FROM subjects WHERE id = @Id",
            new { Id = id.ToString() });
        return row is null ? null : Map(row);
    }

    public async Task<Subject?> GetByCodeAsync(string code)
    {
        using var cn = Open();
        var row = await cn.QueryFirstOrDefaultAsync(
            "SELECT * FROM subjects WHERE code = @Code",
            new { Code = code.ToUpperInvariant() });
        return row is null ? null : Map(row);
    }

    public async Task<Guid> AddAsync(Subject subject)
    {
        const string sql = @"
            INSERT INTO subjects
                (id, code, name, description, credits, max_capacity,
                 current_enrollments, is_active, created_at, updated_at)
            VALUES
                (@Id, @Code, @Name, @Description, @Credits, @MaxCapacity,
                 @CurrentEnrollments, @IsActive, @CreatedAt, @UpdatedAt)";

        using var cn = Open();
        await cn.ExecuteAsync(sql, new
        {
            Id                  = subject.Id.ToString(),
            subject.Code,
            subject.Name,
            subject.Description,
            subject.Credits,
            MaxCapacity         = subject.MaxCapacity,
            CurrentEnrollments  = subject.CurrentEnrollments,
            IsActive            = subject.IsActive ? 1 : 0,
            subject.CreatedAt,
            subject.UpdatedAt,
        });
        return subject.Id;
    }

    public async Task<bool> UpdateAsync(Subject subject)
    {
        const string sql = @"
            UPDATE subjects SET
                name = @Name, description = @Description,
                credits = @Credits, max_capacity = @MaxCapacity,
                current_enrollments = @CurrentEnrollments,
                is_active = @IsActive, updated_at = @UpdatedAt
            WHERE id = @Id";

        using var cn = Open();
        var rows = await cn.ExecuteAsync(sql, new
        {
            subject.Name,
            subject.Description,
            subject.Credits,
            MaxCapacity        = subject.MaxCapacity,
            CurrentEnrollments = subject.CurrentEnrollments,
            IsActive           = subject.IsActive ? 1 : 0,
            subject.UpdatedAt,
            Id                 = subject.Id.ToString(),
        });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var cn = Open();
        var rows = await cn.ExecuteAsync(
            "DELETE FROM subjects WHERE id = @Id",
            new { Id = id.ToString() });
        return rows > 0;
    }

    private static Subject Map(dynamic r) => Subject.Reconstitute(
        id:                 Guid.Parse(r.id.ToString()),
        code:               (string)r.code,
        name:               (string)r.name,
        description:        r.description is DBNull ? null : (string?)r.description,
        credits:            (int)r.credits,
        maxCapacity:        (int)r.max_capacity,
        currentEnrollments: (int)r.current_enrollments,
        isActive:           Convert.ToBoolean(r.is_active),
        createdAt:          (DateTime)r.created_at,
        updatedAt:          (DateTime)r.updated_at);
}
