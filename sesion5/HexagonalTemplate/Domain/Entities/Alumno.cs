namespace GestionAcademica.Domain.Entities;

/// <summary>
/// Entidad con identidad propia.
/// El dominio es el núcleo del hexágono — sin dependencias externas.
/// </summary>
public class Alumno
{
    public int    Id       { get; private set; }
    public string Nombre   { get; private set; } = string.Empty;
    public string Apellidos { get; private set; } = string.Empty;
    public string Email    { get; private set; } = string.Empty;
    public bool   Activo   { get; private set; }

    public string NombreCompleto => $"{Apellidos}, {Nombre}";

    // Constructor privado — EF Core lo necesita para reconstruir objetos
    private Alumno() { }

    // Factory method — única vía de creación válida
    public static Alumno Crear(int id, string nombre, string apellidos, string email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(apellidos))
            throw new ArgumentException("Los apellidos son obligatorios.", nameof(apellidos));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("El email no es válido.", nameof(email));

        return new Alumno
        {
            Id        = id,
            Nombre    = nombre.Trim(),
            Apellidos = apellidos.Trim(),
            Email     = email.Trim().ToLowerInvariant(),
            Activo    = true
        };
    }

    public void Desactivar()
    {
        if (!Activo)
            throw new InvalidOperationException("El alumno ya está inactivo.");
        Activo = false;
    }
}