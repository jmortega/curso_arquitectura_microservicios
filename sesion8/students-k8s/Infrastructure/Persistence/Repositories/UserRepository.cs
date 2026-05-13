using System.Data;
using Dapper;
using MySql.Data.MySqlClient;
using Students.Domain.Entities;
using Students.Domain.Interfaces;

namespace Students.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
        => _connectionString = connectionString;

    private IDbConnection CreateConnection() =>
        new MySqlConnection(_connectionString);

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = @"
            SELECT id, username, email, password_hash, role, is_active, created_at
            FROM users
            WHERE username = @Username AND is_active = 1";

        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(sql, new { Username = username });
        return row is null ? null : MapRow(row);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = @"
            SELECT id, username, email, password_hash, role, is_active, created_at
            FROM users
            WHERE email = @Email AND is_active = 1";

        using var conn = CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(sql, new { Email = email });
        return row is null ? null : MapRow(row);
    }

    public async Task<bool> ExistsAsync(string username, string email)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM users
            WHERE username = @Username OR email = @Email";

        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Username = username, Email = email });
        return count > 0;
    }

    public async Task<Guid> CreateAsync(User user)
    {
        const string sql = @"
            INSERT INTO users (id, username, email, password_hash, role, is_active, created_at)
            VALUES (@Id, @Username, @Email, @PasswordHash, @Role, @IsActive, @CreatedAt)";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            Id           = user.Id.ToString(),
            Username     = user.Username,
            Email        = user.Email,
            PasswordHash = user.PasswordHash,
            Role         = user.Role,
            IsActive     = user.IsActive,
            CreatedAt    = user.CreatedAt,
        });

        return user.Id;
    }

    private static User MapRow(dynamic row) =>
        User.Reconstitute(
            id:           Guid.Parse(row.id.ToString()),
            username:     (string)row.username,
            email:        (string)row.email,
            passwordHash: (string)row.password_hash,
            role:         (string)row.role,
            isActive:     (bool)row.is_active,
            createdAt:    (DateTime)row.created_at);
}
