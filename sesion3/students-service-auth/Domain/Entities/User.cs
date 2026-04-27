namespace Students.Domain.Entities;

public class User
{
    public Guid     Id           { get; private set; }
    public string   Username     { get; private set; }
    public string   Email        { get; private set; }
    public string   PasswordHash { get; private set; }
    public string   Role         { get; private set; }   // Admin | Teacher | ReadOnly
    public bool     IsActive     { get; private set; }
    public DateTime CreatedAt    { get; private set; }

    // Requerido por Dapper
    private User()
    {
        Username     = string.Empty;
        Email        = string.Empty;
        PasswordHash = string.Empty;
        Role         = string.Empty;
    }

    public static User Create(
        string username,
        string email,
        string passwordHash,
        string role = "ReadOnly")
    {
        if (string.IsNullOrWhiteSpace(username))     throw new ArgumentException("El nombre de usuario es obligatorio.");
        if (string.IsNullOrWhiteSpace(email))        throw new ArgumentException("El email es obligatorio.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("La contraseña es obligatoria.");

        return new User
        {
            Id           = Guid.NewGuid(),
            Username     = username.Trim().ToLowerInvariant(),
            Email        = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role         = role,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow,
        };
    }

    // Reconstitución desde base de datos
    public static User Reconstitute(
        Guid id, string username, string email,
        string passwordHash, string role,
        bool isActive, DateTime createdAt)
        => new()
        {
            Id           = id,
            Username     = username,
            Email        = email,
            PasswordHash = passwordHash,
            Role         = role,
            IsActive     = isActive,
            CreatedAt    = createdAt,
        };
}
