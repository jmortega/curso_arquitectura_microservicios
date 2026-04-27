// ── Domain/Entities/Estudiante.cs ─────────────────────────────────────
namespace AcademiaCore.Domain.Entities;

using AcademiaCore.Domain.Common;
using AcademiaCore.Domain.Matriculacion.ValueObjects;

public class Estudiante : Entity<int>
{
    public string  Nombre    { get; private set; } = string.Empty;
    public string  Apellidos { get; private set; } = string.Empty;
    public Email   Email     { get; private set; } = null!;
    public bool    Activo    { get; private set; }

    public string NombreCompleto => $"{Apellidos}, {Nombre}";

    // Constructor sin parámetros requerido por EF Core
    private Estudiante() { }

    public static Estudiante Crear(int id, string nombre,
                                   string apellidos, Email email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(apellidos))
            throw new ArgumentException("Los apellidos son obligatorios.");

        return new Estudiante
        {
            Id        = id,
            Nombre    = nombre.Trim(),
            Apellidos = apellidos.Trim(),
            Email     = email,
            Activo    = true
        };
    }

    public void Desactivar() => Activo = false;

    public void ActualizarEmail(Email nuevoEmail)
    {
        if (!Activo)
            throw new InvalidOperationException(
                "No se puede modificar un estudiante inactivo.");
        Email = nuevoEmail;
    }
}