// ── Domain/Matriculacion/ValueObjects/Email.cs ────────────────────────
namespace AcademiaCore.Domain.Matriculacion.ValueObjects;

using AcademiaCore.Domain.Common;
using System.Text.RegularExpressions;

public sealed class Email : ValueObject
{
    private static readonly Regex FormatoEmail =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Valor { get; }
    
     // Constructor sin parámetros requerido por EF Core
    private Email() { }

    private Email(string valor) => Valor = valor;

    public static Email Crear(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("El email no puede estar vacío.", nameof(valor));

        var email = valor.Trim().ToLowerInvariant();

        if (!FormatoEmail.IsMatch(email))
            throw new ArgumentException(
                $"'{valor}' no es un email válido.", nameof(valor));

        return new Email(email);
    }

    public override string ToString() => Valor;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}