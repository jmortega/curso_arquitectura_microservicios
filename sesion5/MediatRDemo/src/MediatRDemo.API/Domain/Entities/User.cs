namespace MediatRDemo.API.Domain.Entities;

/// <summary>
/// Entidad de dominio Usuario.
/// Contiene las reglas de negocio y los factory methods.
/// </summary>
public sealed class User
{
    public Guid     Id        { get; private set; }
    public string   Name      { get; private set; }
    public string   Email     { get; private set; }
    public string   Role      { get; private set; }   // Admin | User | ReadOnly
    public bool     IsActive  { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Constructor privado — EF Core lo requiere
    private User()
    {
        Name  = string.Empty;
        Email = string.Empty;
        Role  = string.Empty;
    }

    // ── Factory method ────────────────────────────────────────────────────────

    public static User Create(string name, string email, string role = "User")
    {
        if (string.IsNullOrWhiteSpace(name))  throw new ArgumentException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("El email es obligatorio.");

        return new User
        {
            Id        = Guid.NewGuid(),
            Name      = name.Trim(),
            Email     = email.Trim().ToLowerInvariant(),
            Role      = role,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ── Comportamientos de dominio ────────────────────────────────────────────

    public void Update(string name, string email, string role)
    {
        Name      = name.Trim();
        Email     = email.Trim().ToLowerInvariant();
        Role      = role;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;
}
